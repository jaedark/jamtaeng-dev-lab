---
title: "C#에서 객체를 대입할 때 실제로 복사되는 것은 무엇일까?"
description: "값 형식과 참조 형식을 Stack과 Heap이 아닌 복사 의미를 중심으로 다시 정리했습니다."
publishedAt: 2026-07-20
tags: [Concept, CSharp, CLR, Memory]
draft: false
---

C#을 사용하면서 값 형식은 Stack, 참조 형식은 Heap이라고 외우기 쉽습니다. 하지만 이 설명만으로는 실제 코드의 동작을 정확히 설명하기 어렵습니다.

## 먼저 결론

값 형식과 참조 형식의 핵심 차이는 **변수가 무엇을 보관하고, 대입할 때 무엇이 복사되는가**입니다.

```csharp
int first = 10;
int second = first;
second = 20;
```

`second = first`에서는 숫자 값이 복사됩니다. 이후 `second`를 바꿔도 `first`에는 영향이 없습니다.

참조 형식은 다릅니다.

```csharp
var first = new User { Name = "Kim" };
var second = first;
second.Name = "Lee";
```

여기서는 객체가 복제되지 않습니다. 동일한 객체를 가리키는 참조가 복사됩니다.

## Struct는 항상 Stack에 있을까?

그렇지 않습니다. Struct가 Class의 필드라면 관리 Heap에 생성된 객체 내부에 직접 포함될 수 있습니다. 배열 요소로 사용되면 배열 내부에 연속적으로 저장됩니다.

따라서 저장 위치만으로 값 형식과 참조 형식을 구분하는 설명은 충분하지 않습니다.

## 직접 확인한 소스

관련 실행 코드는 저장소의 `samples/csharp/MemoryLab`에 정리했습니다.

## 오늘의 정리

- 값 형식 대입은 값 데이터의 복사입니다.
- 참조 형식 대입은 객체를 가리키는 참조의 복사입니다.
- Struct는 항상 Stack에 존재하지 않습니다.
- 실무에서는 저장 위치보다 복사 의미와 객체 수명을 이해하는 것이 중요합니다.
