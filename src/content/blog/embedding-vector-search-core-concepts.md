---
title: "Embedding과 Vector Search 핵심 개념"
description: "Embedding, Cosine Similarity, Top-K, Vector DB가 어떤 관계인지 빠르게 다시 볼 수 있도록 정리한 개념 노트"
publishedAt: 2026-08-14
tags: [Concept, AI, Embedding, Vector Search]
draft: false
---

FactoryOps AI를 진행하면서 자주 다시 보게 될 개념을 별도로 정리합니다.

## Embedding
Embedding은 텍스트의 의미를 숫자 벡터로 표현하는 방식입니다.

```text
Text
↓
Embedding Model
↓
Vector
```

표현이 달라도 의미가 비슷하면 벡터 공간에서 가까운 위치에 놓이도록 학습됩니다.

예를 들어 다음 두 문장은 문자열은 다르지만 의미는 비슷합니다.

```text
Motor temperature exceeded threshold
장비가 너무 뜨거워졌어
```

## Cosine Similarity
두 벡터가 얼마나 비슷한 방향을 가리키는지 비교하는 방법입니다.

일반적으로 값이 높을수록 의미적으로 더 유사하다고 해석합니다.

FactoryOps AI에서 확인한 예시는 다음과 같습니다.

```text
Motor temperature exceeded threshold
↔ 장비가 너무 뜨거워졌어
Similarity: 0.2578

Motor temperature exceeded threshold
↔ Camera network communication timeout
Similarity: 0.0992
```

절대 점수보다 중요한 것은 관련 문장의 점수가 더 높게 나오는지입니다.

## Vector Search
사용자의 Query도 Embedding하고, 저장된 데이터의 Vector와 비교해 의미적으로 가까운 데이터를 찾는 검색 방식입니다.

```text
Query
↓
Embedding
↓
Similarity Search
↓
Top-K
```

Keyword Search가 같은 문자열을 찾는 데 강하다면 Vector Search는 표현이 달라도 비슷한 의미를 찾는 데 강합니다.

## Top-K
유사도 점수가 높은 결과를 상위 K개만 반환하는 방식입니다.

예를 들어 `top_k=3`이면 가장 가까운 결과 3개만 반환합니다.

## Vector DB가 필요한 이유
소규모 데이터에서는 모든 벡터를 하나씩 비교해도 됩니다.

하지만 데이터가 커지면 검색마다 모든 데이터를 다시 Embedding하거나 전체 벡터를 선형 비교하는 방식은 비효율적입니다.

실무에서는 보통 데이터를 등록할 때 Embedding을 미리 생성해 Vector DB에 저장합니다.

```text
[데이터 적재]
Document
↓
Embedding
↓
Vector DB 저장

[검색]
Query
↓
Query Embedding
↓
Vector DB 검색
↓
Top-K
```

데이터 규모가 더 커지면 ANN Index, Metadata Filtering, Hybrid Search, Reranking 같은 요소가 추가됩니다.

## Keyword Search와 Vector Search
둘 중 하나가 항상 더 좋은 것은 아닙니다.

정확한 장애 코드나 설비명처럼 문자열 일치가 중요한 경우 Keyword Search가 강하고, 자연어 표현처럼 의미가 중요한 경우 Vector Search가 강합니다.

그래서 실제 검색 시스템에서는 둘을 함께 사용하는 Hybrid Search도 자주 고려합니다.

## RAG와의 관계
Vector Search는 RAG의 Retrieval 단계에서 자주 사용됩니다.

```text
사용자 질문
↓
관련 데이터 검색
↓
검색 결과를 LLM Context로 전달
↓
근거 기반 답변 생성
```

즉 Vector Search는 답변을 생성하는 기술이 아니라, LLM에게 전달할 관련 정보를 찾는 기술입니다.
