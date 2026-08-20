---
title: "RAG 검색에 Reranker를 붙였는데 성능이 떨어진 이유"
description: "Vector Search와 RRF 뒤에 Cross Encoder Reranker를 적용하고 Hit@1, Hit@3, MRR로 평가한 결과와 미채택 판단 과정을 정리합니다."
publishedAt: 2026-08-20
tags: ["Concept", "AI", "RAG", "Search", "Reranking", "Cross Encoder", "RRF", "Evaluation"]
draft: false
---

RAG 검색 품질을 개선하다 보면 자연스럽게 **Reranker(재순위화 모델)** 를 붙이고 싶어집니다.

Retriever(검색기)가 관련 문서를 찾고, Cross Encoder(교차 인코더)가 그 후보를 더 정밀하게 다시 정렬하면 당연히 검색 성능도 좋아질 것처럼 보입니다.

하지만 실제로 평가해보니 결과는 달랐습니다.

이번 FactoryOps AI 실험에서는 기존에 틀리던 검색 3건을 모두 개선했지만, 반대로 기존에 잘 찾던 검색 결과가 나빠졌습니다.

결론부터 말하면 **Reranker를 구현했지만 현재 기본 검색 경로에는 채택하지 않았습니다.**

## 먼저 쉬운 평가 데이터부터 의심했다

기존 Retrieval Evaluation(검색 평가)에서는 Vector Search와 RRF가 모두 높은 결과를 보였습니다.

문제는 평가 Query가 너무 쉬우면 검색 방식의 차이를 확인하기 어렵다는 것입니다.

그래서 장비명을 직접 노출하지 않고 동의어, 구어체, 증상 중심 표현을 사용하는 Hard Query(어려운 평가 질의) 10개를 따로 만들었습니다.

예를 들면 다음과 같습니다.

```text
모터 쪽 열이 계속 올라가는데 냉각 계통부터 봐야 할까?
```

Ground Truth(정답 기준 데이터)는 `Conveyor-01`입니다.

또 다른 예시는 다음과 같습니다.

```text
계속 고장난 건 아닌데 가끔 센서 입력이 아예 안 잡혀
```

Ground Truth는 `Sensor-05`입니다.

이런 Query를 이용해 기존 검색 방식을 다시 평가했습니다.

## Hard Retrieval Evaluation 결과

평가 지표는 세 가지를 사용했습니다.

- **Hit@1**: 정답이 검색 결과 1위에 있는 비율
- **Hit@3**: 정답이 상위 3개 안에 있는 비율
- **MRR(Mean Reciprocal Rank, 평균 역순위)**: 정답이 얼마나 앞쪽 순위에 배치되는지 나타내는 값

결과는 다음과 같았습니다.

| 검색 방식 | Hit@1 | Hit@3 | MRR |
|---|---:|---:|---:|
| Vector Search | 60% | 100% | 0.8000 |
| RRF | 70% | 100% | 0.8500 |

여기서 가장 눈에 띈 값은 `Hit@3 = 100%`였습니다.

즉 검색기가 정답을 완전히 놓치고 있는 것이 아니라, **모든 정답을 Top 3 안에는 가져오지만 일부를 2위에 배치하고 있었습니다.**

이 경우 Retriever 자체의 Recall(재현율) 문제보다는 Ranking Quality(순위 품질) 문제가 더 크다고 볼 수 있습니다.

그래서 Reranking을 실험하기로 했습니다.

## Retriever와 Reranker의 역할은 다르다

Retriever는 전체 데이터에서 관련 있을 가능성이 높은 후보를 빠르게 찾습니다.

```text
전체 Incident
    ↓
Retriever
    ↓
Top-N Candidates
```

Reranker는 이미 찾은 후보만 더 정밀하게 비교해서 순서를 바꿉니다.

```text
Top-N Candidates
    ↓
Reranker
    ↓
Top-K Results
```

이번 실험에서는 다음 구조를 사용했습니다.

```text
Query
  ↓
Vector / RRF Retriever
  ↓
Top 5
  ↓
Cross Encoder Reranker
  ↓
Top 3
```

중요한 점은 Reranker가 Retriever가 가져오지 않은 문서를 다시 찾아주는 것은 아니라는 것입니다.

정답이 Top-N 후보에 없다면 Reranker도 해결할 수 없습니다.

## Bi-Encoder와 Cross Encoder 차이

Vector Search에서 사용하는 Embedding 기반 검색은 Query와 Document를 각각 따로 벡터로 변환합니다.

```text
Query    → Embedding
Document → Embedding
           ↓
     Cosine Similarity
```

이 방식은 빠르기 때문에 대량 검색에 적합합니다.

반면 Cross Encoder는 Query와 Document를 하나의 Pair(쌍)로 넣습니다.

```text
Query + Document
       ↓
Cross Encoder
       ↓
Relevance Score
```

두 텍스트를 함께 보면서 관련성을 계산하기 때문에 더 정밀한 순위 판단을 기대할 수 있지만, 계산 비용은 더 큽니다.

그래서 일반적으로 전체 데이터에 Cross Encoder를 적용하기보다 Retriever가 먼저 후보를 줄이고 그 후보만 Reranking합니다.

## Cross Encoder Reranker 구현

이번 실험에서 사용한 모델은 다음과 같습니다.

```text
cross-encoder/mmarco-mMiniLMv2-L12-H384-v1
```

Retriever의 Top-N은 5, 최종 Top-K는 3으로 설정했습니다.

Reranker는 별도의 서비스로 분리했습니다.

개념적으로는 Query와 각 Incident를 Pair로 만들고 점수를 계산합니다.

```python
pairs = [
    [query, document]
    for document in documents
]

scores = model.predict(pairs)
```

Incident는 다음 필드를 하나의 문서 문자열로 구성했습니다.

```text
Equipment
Process
Symptom
Cause
Action
Result
```

모델 이름도 코드 곳곳에 직접 작성하지 않고 Config로 분리했습니다.

## 최종 비교 결과

동일한 Hard Query 10개로 네 가지 검색 방식을 비교했습니다.

| 검색 방식 | Hit@1 | Hit@3 | MRR |
|---|---:|---:|---:|
| Vector | 60% | 100% | 0.8000 |
| RRF | 70% | 100% | 0.8500 |
| Vector + Rerank | 70% | 90% | 0.8000 |
| RRF + Rerank | 70% | 90% | 0.8000 |

Reranker를 적용하면 Hit@1이 개선될 것으로 기대했습니다.

실제로 일부 Query는 개선됐습니다.

하지만 전체 결과를 보면 가장 좋은 방식은 여전히 RRF였습니다.

특히 Reranking 이후 Hit@3가 `100% → 90%`로 하락했고, RRF 대비 MRR도 `0.85 → 0.80`으로 낮아졌습니다.

## 기존 실패 3건은 모두 개선됐다

흥미로운 점은 Reranker가 기존의 대표적인 실패 사례는 정확히 고쳤다는 것입니다.

### Sensor-05

```text
Before: 2위
After : 1위
```

### Vision-02

```text
Before: 2위
After : 1위
```

### Sensor-03

```text
Before: 2위
After : 1위
```

즉 Reranker 자체가 의미 없는 것은 아니었습니다.

Vector Search가 비슷한 의미의 장애를 혼동한 상황에서는 Query와 Document를 함께 비교하는 Cross Encoder가 더 좋은 판단을 한 사례가 있었습니다.

## 그런데 새로운 Regression이 생겼다

문제는 기존에 정상적으로 검색되던 Query가 나빠졌다는 것입니다.

Regression(회귀)은 변경 이후 기존에 잘 동작하던 결과가 악화되는 현상을 의미합니다.

실험에서는 다음 문제가 발생했습니다.

```text
Robot-02
1위 → 2위

PLC-01
1위 → 2위
```

그리고 가장 큰 문제는 `Vision-01`이었습니다.

```text
Before Reranking
Top 1

After Reranking
Top 3 밖
```

이 한 건 때문에 Hit@3가 100%에서 90%로 떨어졌습니다.

Reranker가 기존 실패만 개선하는 것이 아니라 **기존 정답 순위를 다시 흔들 수도 있다는 것**을 확인했습니다.

## RRF 실패에서도 배운 점

RRF 실패 사례를 보면 Keyword Search의 순위가 `None`인 경우가 많았습니다.

즉 Keyword Retriever가 아무런 유효 신호를 제공하지 못하면 RRF는 사실상 Vector Search의 순위를 거의 그대로 따라가게 됩니다.

```text
Keyword Signal 없음
       ↓
RRF가 결합할 추가 정보 부족
       ↓
Vector Ranking과 비슷한 결과
```

RRF 역시 항상 좋아지는 기술이 아니라 **서로 다른 Retriever가 유용하고 상호보완적인 검색 신호를 제공할 때 의미가 커진다**는 점을 확인했습니다.

## 정확도뿐 아니라 Latency도 비용이다

Reranker에는 추가 계산 비용도 있었습니다.

이번 환경에서 측정한 값은 다음과 같습니다.

```text
최초 다운로드 / 모델 로딩: 22.4297초
캐시 이후 모델 로딩:       1.9604초
Hard Evaluation 전체:     19.3965초
```

실제 서비스에서는 요청당 모델을 다시 로딩하지 않도록 해야 하지만, Reranking 추론 자체가 추가 Latency(지연시간)를 만드는 것은 피할 수 없습니다.

정확도가 크게 개선된다면 감수할 수 있는 비용입니다.

하지만 현재 평가에서는 정확도까지 개선되지 않았기 때문에 복잡도와 Latency를 추가할 근거가 부족했습니다.

## 그래서 Reranker를 채택하지 않았다

현재 평가 데이터 기준 최종 선택은 RRF입니다.

```text
Keyword Search
      +
Vector Search
      ↓
     RRF
      ↓
    Top-K
      ↓
     RAG
```

Reranker 구현은 삭제하지 않았습니다.

향후 다음 상황에서는 다시 평가할 수 있습니다.

- Incident 데이터가 충분히 증가했을 때
- Hard Query 평가셋이 커졌을 때
- 다른 Multilingual Cross Encoder 모델을 비교할 때
- 검색 도메인에 맞는 Reranker를 사용할 수 있을 때

하지만 현재 기준으로는 기본 Pipeline에 넣지 않는 것이 더 합리적이라고 판단했습니다.

## 이번 실험에서 가장 크게 배운 점

이번 단계에서 가장 중요한 학습은 Cross Encoder 사용법 자체가 아니었습니다.

```text
새 기술이 좋아 보인다
       ↓
구현한다
       ↓
Evaluation 한다
       ↓
전체 Metric을 비교한다
       ↓
채택 또는 미채택한다
```

처음에는 Reranker가 Vector Search보다 정밀한 모델이므로 당연히 전체 검색 품질도 좋아질 것이라고 생각했습니다.

실제 결과는 달랐습니다.

**일부 Query를 개선하는 것과 시스템 전체 성능을 개선하는 것은 같은 의미가 아니었습니다.**

특히 검색/RAG 시스템에서는 새 기술을 추가한 사실보다 그 기술이 실제 데이터에서 어떤 영향을 주는지를 검증하는 과정이 더 중요하다는 것을 확인했습니다.

이번 결과를 기준으로 Retrieval 계층은 일단 고정하고, 다음 단계에서는 **Tool Calling(도구 호출)** 으로 넘어갈 예정입니다.
