---
title: "RAG란 무엇인가? Retrieval부터 Grounding까지"
description: "RAG의 Retrieval, Augmentation, Generation 구조와 Context, Grounding, Sources, 검색 품질의 관계를 프로젝트와 분리해 정리한 기술 노트"
publishedAt: 2026-08-14
tags: [Concept, AI, RAG, LLM, Retrieval, Grounding]
draft: false
---

RAG는 **Retrieval-Augmented Generation**의 약자입니다.

LLM이 자신의 학습 지식만으로 답하지 않고, 외부 데이터에서 관련 정보를 먼저 검색한 뒤 그 정보를 근거로 답변을 생성하도록 만드는 구조입니다.

```text
사용자 질문
↓
Retrieval
↓
관련 데이터 검색
↓
Augmentation
↓
검색 결과를 Context에 추가
↓
Generation
↓
LLM 답변 생성
```

## 왜 RAG가 필요한가?

LLM에게 질문만 전달하면 모델은 학습 과정에서 얻은 일반 지식을 이용해 답합니다.

하지만 실제 서비스에서는 다음과 같은 정보가 필요할 수 있습니다.

- 사내 기술 문서
- 장애 이력
- 제품 매뉴얼
- 고객 문서
- 최신 정책
- 데이터베이스의 업무 데이터

이 정보는 LLM이 학습하지 않았거나 최신 상태가 아닐 수 있습니다.

RAG는 필요한 정보를 먼저 검색해 LLM에게 전달함으로써 이 문제를 줄입니다.

## 1. Retrieval

Retrieval은 질문과 관련 있는 정보를 찾는 단계입니다.

예를 들어 사용자가 다음과 같이 질문했다고 가정합니다.

```text
장비가 너무 뜨거워졌어. 무엇을 확인해야 해?
```

검색 시스템은 저장된 데이터 중 관련 문서를 찾습니다.

```text
Symptom: Motor temperature exceeded threshold
Cause: Cooling fan malfunction
Action: Replaced cooling fan
Result: Temperature returned to normal
```

Retrieval에는 여러 방법을 사용할 수 있습니다.

```text
Keyword Search
Vector Search
Hybrid Search
Metadata Filtering
Reranking
```

RAG가 반드시 Vector Search를 의미하는 것은 아닙니다. 중요한 것은 **질문에 필요한 근거를 얼마나 잘 검색하느냐**입니다.

## 2. Augmentation

검색 결과를 찾았다고 바로 끝나는 것은 아닙니다.

검색된 데이터를 LLM이 이해할 수 있도록 Prompt의 Context에 포함해야 합니다.

예를 들어:

```text
[Context]
Symptom: Motor temperature exceeded threshold
Cause: Cooling fan malfunction
Action: Replaced cooling fan

[Question]
장비가 너무 뜨거워졌어. 무엇을 확인해야 해?
```

이처럼 원래 질문에 검색된 정보를 보강하는 과정이 Augmentation입니다.

실제 시스템에서는 Context의 양과 순서도 중요합니다. 관련성이 낮은 자료를 너무 많이 넣으면 오히려 답변 품질이 떨어질 수 있습니다.

## 3. Generation

마지막 단계에서는 LLM이 질문과 Context를 읽고 답변을 생성합니다.

```text
Question + Retrieved Context
↓
LLM
↓
근거 기반 Answer
```

예를 들면 다음과 같은 답변을 만들 수 있습니다.

```text
예상 원인: 냉각 팬 오작동
확인 항목: 냉각 팬 작동 상태
권장 조치: 냉각 팬 점검 또는 교체
```

여기서 중요한 점은 LLM이 검색을 수행한 것이 아니라는 것입니다.

```text
검색 시스템
→ 필요한 자료 선택

LLM
→ 선택된 자료를 이용해 설명 생성
```

두 역할은 분리되어 있습니다.

## Embedding Model과 LLM은 역할이 다르다

Vector Search 기반 RAG를 구성하면 보통 두 종류의 모델이 등장합니다.

```text
Embedding Model
→ Text를 Vector로 변환
→ Retrieval에 사용

LLM
→ Context를 읽고 자연어 생성
→ Generation에 사용
```

따라서 검색용 Embedding 모델과 답변 생성용 LLM은 서로 다른 모델을 사용해도 됩니다.

## Grounding

RAG를 사용한다고 Hallucination이 자동으로 사라지는 것은 아닙니다.

LLM이 제공된 Context를 무시하거나 자신의 일반 지식을 섞어 답할 수도 있습니다.

그래서 Prompt에 다음과 같은 규칙을 추가할 수 있습니다.

```text
제공된 자료를 근거로만 답변하세요.
자료에 없는 내용은 임의로 추측하지 마세요.
근거가 부족하면 판단하기 어렵다고 답변하세요.
```

이처럼 모델의 답변을 제공된 근거에 묶는 것을 **Grounding** 관점에서 볼 수 있습니다.

## Sources와 Traceability

RAG 서비스에서는 Answer만 반환하는 것보다 어떤 자료를 사용했는지도 함께 반환하는 것이 좋습니다.

```json
{
  "answer": "냉각 팬 상태를 우선 확인하세요.",
  "sources": [
    {
      "document_id": 12,
      "similarity": 0.48
    }
  ]
}
```

이렇게 하면 사용자는 다음을 확인할 수 있습니다.

```text
왜 이런 답을 했는가?
↓
어떤 문서를 검색했는가?
↓
그 근거가 실제로 적절한가?
```

이런 추적 가능성을 **Traceability**라고 볼 수 있습니다.

## Top-K만으로 충분하지 않은 이유

Vector Search에서 흔히 상위 K개의 결과를 가져옵니다.

```text
Top 1  0.48
Top 2  0.37
Top 3  0.34
```

하지만 Top-K는 단순히 상대적으로 높은 결과를 가져오는 방식입니다.

3위 결과가 실제로 질문과 거의 관련이 없더라도 `top_k=3`이면 Context에 들어갈 수 있습니다.

그래서 다음과 같은 방법을 함께 사용합니다.

```text
Similarity Threshold
Metadata Filtering
Hybrid Search
Reranking
Evaluation
```

## Similarity Threshold

Similarity Threshold는 일정 유사도 이하의 검색 결과를 제거하는 방법입니다.

예를 들어 threshold가 `0.4`라면:

```text
0.48 → 포함
0.37 → 제외
0.34 → 제외
```

Context에 불필요한 데이터를 줄일 수 있습니다.

하지만 `0.4` 같은 값이 모든 데이터에 적용되는 정답은 아닙니다. Embedding 모델, 문서 구조, 질의 형태에 따라 점수 분포가 달라지므로 Evaluation을 통해 조정해야 합니다.

## 자동 테스트에서 실제 LLM을 호출하면 안 되는 이유

RAG를 테스트할 때 매번 실제 LLM API를 호출하면 테스트가 외부 서비스에 의존하게 됩니다.

```text
네트워크 장애
API Rate Limit
비용 또는 무료 사용량
LLM 응답의 비결정성
```

따라서 단위 테스트에서는 보통 LLM 호출 부분을 Mock 처리하고, RAG Pipeline 자체가 원하는 구조로 동작하는지 검증합니다.

```text
Retrieval        실제 테스트
Context Builder  실제 테스트
Prompt Builder   실제 테스트
LLM API          Mock
```

## RAG 품질을 결정하는 것은 LLM만이 아니다

RAG 답변이 좋지 않을 때 흔히 LLM 모델부터 바꾸고 싶어집니다.

하지만 실제로는 앞단의 검색 품질이 원인일 수 있습니다.

```text
좋지 않은 Retrieval
↓
좋지 않은 Context
↓
좋지 않은 Generation
```

즉 RAG에서는 다음 요소를 함께 봐야 합니다.

```text
Retrieval Quality
Context Quality
Prompt / Grounding
LLM Generation
Evaluation
```

## 한 문장으로 정리

**RAG는 외부 데이터에서 필요한 근거를 검색하고, 그 근거를 LLM의 Context에 추가해 더 신뢰할 수 있는 답변을 생성하도록 만드는 아키텍처 패턴입니다.**

다음에 RAG를 다시 볼 때는 다음 흐름부터 떠올리면 됩니다.

```text
Retrieve
→ Filter / Rank
→ Build Context
→ Ground Prompt
→ Generate
→ Return Sources
→ Evaluate
```
