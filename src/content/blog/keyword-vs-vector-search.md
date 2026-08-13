---
title: "왜 Keyword Search 다음에 Vector Search를 만들었을까?"
description: "제조 장애 검색에서 문자열 검색의 한계를 확인하고, Embedding과 Cosine Similarity로 의미 기반 Vector Search를 구현한 과정"
publishedAt: 2026-08-13
tags:
  - AI
  - Vector Search
  - Embedding
  - FastAPI
  - RAG
draft: false
---

FactoryOps AI를 만들면서 처음부터 RAG나 Vector DB를 붙이지 않고, 먼저 Keyword Search부터 구현했습니다.

이유는 단순합니다.

**Vector Search가 왜 필요한지 직접 확인하고 싶었기 때문입니다.**

이번 단계에서는 Keyword Search가 놓치는 자연어 표현을 Vector Search가 실제로 찾아낼 수 있는지 테스트해봤습니다.

## Keyword Search에서 바로 드러난 문제

샘플 장애 데이터에는 이런 내용이 있습니다.

```text
Equipment: Conveyor-01
Symptom: Motor temperature exceeded threshold
Cause: Cooling fan malfunction
```

여기에 사용자가 이렇게 검색한다고 가정했습니다.

```text
장비가 너무 뜨거워졌어
```

Keyword Search 결과는 없었습니다.

```text
Keyword Search: 결과 없음
```

당연한 결과입니다.

DB에는 `뜨거워졌어`라는 문자열이 없고, 저장된 장애 설명은 영어로 `Motor temperature exceeded threshold`라고 되어 있기 때문입니다.

사람은 두 문장이 비슷한 의미라는 것을 쉽게 이해하지만 문자열 검색은 그렇지 못합니다.

이 문제가 Vector Search를 도입한 이유였습니다.

## Embedding은 문장의 의미를 숫자로 바꾼다

Vector Search를 구현하려면 먼저 문장을 벡터로 변환해야 합니다.

이번에는 다국어 검색을 위해 다음 Sentence Transformer 모델을 사용했습니다.

```text
sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
```

개념적으로는 다음과 같습니다.

```text
Motor temperature exceeded threshold
        ↓
Embedding Model
        ↓
[0.021, -0.142, 0.085, ...]
```

한국어 질의도 동일하게 벡터로 변환됩니다.

```text
장비가 너무 뜨거워졌어
        ↓
Embedding Model
        ↓
[...]
```

이제 문자열 자체가 아니라 벡터끼리 비교할 수 있습니다.

## Cosine Similarity로 직접 비교해보기

먼저 작은 테스트부터 해봤습니다.

기준 문장은 다음과 같습니다.

```text
Motor temperature exceeded threshold
```

비교할 문장 두 개를 준비했습니다.

```text
장비가 너무 뜨거워졌어
Camera network communication timeout
```

결과는 다음과 같았습니다.

```text
Motor temperature exceeded threshold
↔ 장비가 너무 뜨거워졌어
Similarity: 0.2578

Motor temperature exceeded threshold
↔ Camera network communication timeout
Similarity: 0.0992
```

점수 자체가 높으냐보다 중요한 것은 **상대적인 순서**였습니다.

표현과 언어가 다른데도 `장비가 너무 뜨거워졌어`가 카메라 통신 장애보다 온도 장애에 더 가까운 문장으로 판단됐습니다.

이제 의미 검색을 실제 장애 데이터 전체에 적용해볼 수 있었습니다.

## 장애 데이터 전체를 대상으로 Top-K 검색

현재는 데이터가 10건뿐이라 별도의 Vector DB를 사용하지 않았습니다.

구조는 단순합니다.

```text
사용자 Query
    ↓
Query Embedding
    ↓
장애 데이터 조회
    ↓
각 장애 Embedding
    ↓
Cosine Similarity 계산
    ↓
유사도 순 정렬
    ↓
Top-K 반환
```

장애를 Embedding할 때는 `symptom` 하나만 사용하지 않고 다음 정보를 하나의 텍스트로 합쳤습니다.

```text
Equipment
Process
Symptom
Cause
Action
Result
```

그리고 FastAPI에 의미 검색 API를 추가했습니다.

```text
GET /incidents/vector-search
```

예를 들어 다음 질의를 보냅니다.

```text
query = 장비가 너무 뜨거워졌어
top_k = 3
```

결과는 다음과 같았습니다.

```text
1. Conveyor-01 - 0.2761
2. Conveyor-02 - 0.1505
3. Sensor-03 - 0.1300
```

가장 관련 있는 `Conveyor-01 / Motor temperature exceeded threshold`가 1위로 검색됐습니다.

## Keyword Search와 비교해보니 차이가 더 명확했다

여러 자연어 질의를 비교했습니다.

```text
[Query] 장비가 너무 뜨거워졌어
Keyword Search: 결과 없음
Vector Search Top1: Conveyor-01 / Motor temperature exceeded threshold / 0.2761

[Query] 카메라 영상이 안 들어와
Keyword Search: 결과 없음
Vector Search Top1: Vision-02 / Inspection accuracy decreased / 0.2551

[Query] 로봇 위치가 이상해
Keyword Search: 결과 없음
Vector Search Top1: Robot-01 / Robot position deviation detected / 0.4400

[Query] 센서가 가끔 감지를 못해
Keyword Search: 결과 없음
Vector Search Top1: Sensor-05 / Sensor signal not detected / 0.2858
```

Keyword Search는 네 질의 모두 결과를 찾지 못했습니다.

반면 Vector Search는 표현이 달라도 의미적으로 관련 있는 장애 데이터를 상위에 올려줬습니다.

이번 실습에서 가장 확실하게 느낀 차이는 이것이었습니다.

> Keyword Search는 같은 단어를 찾는 데 강하고, Vector Search는 비슷한 의미를 찾는 데 강하다.

## Vector Search도 항상 정답은 아니었다

흥미로운 결과도 있었습니다.

```text
카메라 영상이 안 들어와
```

라는 질의에 제가 기대한 결과는 `Vision-01 / Camera acquisition timeout`이었습니다.

하지만 실제 Top1은 다음 데이터였습니다.

```text
Vision-02 / Inspection accuracy decreased
```

완전히 엉뚱한 분야는 아니지만 원하는 결과와는 조금 달랐습니다.

이 결과를 보면서 Vector Search 역시 그냥 적용한다고 끝나는 기술은 아니라는 것을 확인했습니다.

현재는 장애 전체 문맥을 하나로 합쳐 Embedding하고 있기 때문에 `Vision`, `Inspection` 등 다른 정보의 영향도 함께 받습니다.

앞으로는 이런 방식들을 비교해볼 수 있습니다.

- symptom만 Embedding
- symptom + cause
- symptom + cause + action
- Keyword + Vector를 결합한 Hybrid Search
- 검색 결과에 대한 Reranking

검색 시스템에서는 결국 **검색이 되느냐**뿐만 아니라 **원하는 결과가 얼마나 높은 순위에 나오느냐**가 중요합니다.

## 테스트로 검색 동작을 고정했다

Vector Search 구현 후 pytest도 추가했습니다.

검증한 내용은 다음과 같습니다.

```text
Vector Search API 정상 동작
Top-K 개수 적용
자연어 질의에 관련 장애 검색
Keyword Search가 실패한 질의를 Vector Search가 검색
```

최종 결과는:

```text
10 passed
```

였습니다.

특히 아래 상황을 테스트로 남겼습니다.

```text
"장비가 너무 뜨거워졌어"

Keyword Search → []
Vector Search  → Conveyor-01
```

단순히 API가 동작하는 것보다 **왜 이 기능을 추가했는지 테스트 자체가 보여주도록** 만들었습니다.

## 그런데 실제 대용량 환경에서도 이렇게 검색할까?

원리는 같습니다.

하지만 지금처럼 검색할 때마다 모든 데이터를 다시 Embedding하고 전부 비교하지는 않습니다.

현재 데이터가 10건이라면 문제가 없지만 데이터가 10만 건, 100만 건이 되면 비효율적입니다.

실제 구조는 보통 다음 방향으로 발전합니다.

```text
[데이터 등록]

Incident / Document
       ↓
Embedding 생성
       ↓
Vector DB 저장


[검색]

사용자 Query
       ↓
Query만 Embedding
       ↓
Vector DB
       ↓
유사 Vector 검색
       ↓
Top-K
```

그리고 데이터가 커지면 ANN Index, Metadata Filtering, Hybrid Search, Reranking 같은 기술이 추가됩니다.

즉 이번에 만든 것은 실무 Vector Search 구조의 아주 작은 버전입니다.

처음부터 Vector DB를 붙이지 않은 이유도 여기에 있습니다.

직접 Cosine Similarity로 검색을 구현해보니 나중에 Vector DB를 사용하는 이유가 훨씬 명확해졌습니다.

## 다음은 RAG

현재 FactoryOps AI는 여기까지 왔습니다.

```text
사용자 질문
    ↓
관련 장애 검색
```

다음 단계인 RAG에서는 검색 결과를 LLM에 전달합니다.

```text
사용자 질문
    ↓
Vector Search
    ↓
관련 장애 / 문서
    ↓
LLM Context
    ↓
원인 및 대응 방법 생성
```

이번에 구현한 Vector Search가 바로 RAG의 **Retrieval** 부분입니다.

다음에는 단순히 관련 장애를 찾아주는 것을 넘어, 검색된 장애 이력을 근거로 AI가 장애 원인과 대응 방법을 설명하도록 만들어볼 예정입니다.
