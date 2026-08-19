---
title: "Hybrid Search와 RRF: 검색 결과를 어떻게 결합하고 평가할까?"
description: "Keyword Search와 Vector Search를 결합하는 Weighted Hybrid, RRF, 그리고 Hit@1·Hit@3·MRR 기반 Retrieval Evaluation을 정리합니다."
publishedAt: 2026-08-19
tags: ["Concept", "AI", "RAG", "Search", "Hybrid Search", "RRF", "Evaluation"]
draft: false
---

RAG를 만들다 보면 자연스럽게 이런 문제가 생깁니다.

- 정확한 설비명이나 코드처럼 문자열이 중요한 검색
- 표현은 다르지만 의미가 같은 자연어 검색

둘 중 하나만 잘해서는 부족합니다.

이때 자주 등장하는 접근이 **Hybrid Search**입니다.

## Keyword Search와 Vector Search의 차이

Keyword Search는 정확한 문자열에 강합니다.

예를 들어 `Sensor-05`, `PLC-01`, `ERR-1023` 같은 값은 의미를 추론하는 것보다 정확히 일치하는지를 보는 것이 중요합니다.

반대로 Vector Search는 표현이 달라도 의미가 비슷한 문장을 찾는 데 강합니다.

예를 들어 다음 두 문장은 문자열은 다르지만 의미는 가깝습니다.

```text
장비가 너무 뜨거워졌어
Motor temperature exceeded threshold
```

그래서 둘의 장점을 결합하는 것이 Hybrid Search의 출발점입니다.

## Weighted Hybrid Search

가장 이해하기 쉬운 방법은 점수를 직접 합치는 것입니다.

```text
Hybrid Score
= Keyword Score × 0.4
+ Vector Score × 0.6
```

장점은 단순하다는 것입니다.

하지만 곧 문제가 생깁니다.

Keyword Score와 Vector Similarity가 정말 같은 척도일까요?

예를 들어 Keyword Score의 0.8과 Vector Similarity의 0.8은 의미가 전혀 다를 수 있습니다. 모델을 바꾸면 Vector Score의 분포 자체도 달라질 수 있습니다.

따라서 단순 가중합은 구현은 쉽지만 가중치 튜닝에 대한 근거가 필요합니다.

## Token 기반 Keyword Matching

전체 Query를 하나의 문자열로 비교하면 이런 질의가 문제가 됩니다.

```text
Robot-01 위치 문제
```

`Robot-01 위치 문제` 전체 문자열은 설비명 필드에 존재하지 않기 때문에 exact match를 놓칠 수 있습니다.

그래서 Query를 Token 단위로 분리합니다.

```text
Robot-01
위치
문제
```

이렇게 하면 `Robot-01`처럼 정확한 식별자를 강한 신호로 사용할 수 있습니다.

특히 제조·운영 시스템에서는 다음 값들이 의미 검색보다 lexical matching이 더 중요한 경우가 많습니다.

- Equipment ID
- Error Code
- Alarm Code
- Model Name
- Process ID

## RRF란?

RRF는 **Reciprocal Rank Fusion**의 약자입니다.

Weighted Hybrid와 가장 큰 차이는 원래 점수를 직접 합치지 않는다는 것입니다.

대신 각 검색기에서 얻은 **순위(rank)** 를 이용합니다.

```text
RRF Score = 1 / (k + rank)
```

예를 들어 어떤 문서가:

```text
Keyword Search: 1위
Vector Search: 3위
```

라면 두 순위에 따른 RRF 점수를 더합니다.

이 방식의 장점은 Keyword Search와 Vector Search처럼 점수 체계가 서로 다른 검색 결과도 상대적으로 쉽게 결합할 수 있다는 것입니다.

## Weighted Hybrid와 RRF의 차이

```text
Weighted Hybrid
→ 검색기의 실제 점수를 가중합

RRF
→ 검색기의 점수 대신 순위를 결합
```

Weighted Hybrid는 각 점수를 세밀하게 활용할 수 있지만 정규화와 가중치 튜닝이 중요합니다.

RRF는 점수 분포에 덜 의존하기 때문에 서로 다른 Retriever를 결합할 때 사용하기 편합니다.

## RRF가 항상 더 좋은 것은 아니다

중요한 점은 새로운 검색 기법을 구현했다고 해서 반드시 기존 검색 방식을 교체해야 하는 것은 아니라는 것입니다.

예를 들어 평가 결과가 다음과 같다고 가정해보겠습니다.

```text
Vector Search
Hit@1: 100%
Hit@3: 100%
MRR: 1.0000

RRF
Hit@1: 100%
Hit@3: 100%
MRR: 1.0000
```

이 결과만으로는 RRF가 Vector Search보다 더 좋다고 말할 수 없습니다.

평가 데이터가 너무 쉬웠을 수도 있습니다.

따라서 검색 시스템에서는 **기술 추가보다 Evaluation이 중요합니다.**

## Hit@1과 Hit@3

Hit@K는 정답이 상위 K개 결과 안에 존재하는지를 평가합니다.

예를 들어 평가 Query가 100개이고 그중 90개의 정답이 검색 결과 1위에 있었다면:

```text
Hit@1 = 90%
```

정답이 Top 3 안에 98개 있었다면:

```text
Hit@3 = 98%
```

RAG에서는 Context에 몇 개의 문서를 전달할 것인지와도 직접적으로 연결되는 지표입니다.

## MRR

MRR은 **Mean Reciprocal Rank**입니다.

정답의 순위를 역수로 변환합니다.

```text
1위 → 1 / 1 = 1.0
2위 → 1 / 2 = 0.5
3위 → 1 / 3 ≈ 0.333
검색 실패 → 0
```

각 Query의 값을 평균내면 MRR이 됩니다.

즉 단순히 정답을 찾았는지만 보는 것이 아니라 **정답을 얼마나 앞쪽에 배치했는가**를 평가할 수 있습니다.

## 좋은 Evaluation Dataset이 중요하다

평가 Query가 너무 쉬우면 모든 검색 방식이 100%를 기록할 수 있습니다.

좋은 평가 데이터는 오히려 시스템의 약점을 드러내야 합니다.

예를 들어 다음처럼 비슷한 Incident가 여러 개 존재하는 상황이 유용합니다.

```text
센서가 가끔 감지를 못해
센서 신호가 아예 안 잡혀
검사 정확도가 떨어져
카메라 영상이 안 들어와
```

이런 질의는 단순 문자열이나 단순 Vector Similarity만으로 구분하기 어려울 수 있습니다.

## Retrieval 개선의 기본 흐름

검색 시스템을 개선할 때는 다음 흐름이 유용합니다.

```text
Keyword Search
↓
Vector Search
↓
Hybrid Search
↓
RRF
↓
Evaluation
↓
Reranking
```

핵심은 순서대로 기술을 추가하는 것이 아닙니다.

각 단계에서 **현재 방식의 한계를 확인하고, 다음 기술이 실제로 필요한 이유를 만든 뒤 도입하는 것**입니다.

## 다음 단계: Reranking

Retriever가 Top-N 후보를 찾은 뒤, Query와 후보 문서를 더 정밀하게 비교해 순서를 다시 정하는 방식이 Reranking입니다.

```text
Query
↓
Retriever
↓
Top-N
↓
Reranker
↓
Top-K
↓
RAG Context
```

Vector Search나 RRF가 후보를 빠르게 좁히는 역할이라면, Reranker는 그 후보 안에서 더 정확한 순서를 만드는 역할을 합니다.

검색 품질을 높이는 과정에서 다음으로 살펴볼 자연스러운 단계입니다.

## 정리

Hybrid Search의 핵심은 Keyword와 Vector를 단순히 같이 쓴다는 데 있지 않습니다.

**정확한 문자열 검색과 의미 검색이라는 서로 다른 강점을 어떤 방식으로 결합하고, 실제로 더 좋아졌는지 어떻게 측정할 것인가**가 핵심입니다.

검색 시스템은 감으로 좋아 보인다고 판단하기보다, Ground Truth와 Evaluation Metric을 이용해 개선 효과를 검증하는 것이 중요합니다.