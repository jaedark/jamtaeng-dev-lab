---
title: "C# 개발 관점에서 시작한 Python 첫 실습"
description: "Python의 dataclass, 타입 힌트와 함수 구조를 C# 코드와 비교해 보았습니다."
publishedAt: 2026-07-20
tags: [Python, Learning]
draft: false
---

Python을 새로운 언어로 외우기보다 이미 알고 있는 C# 개념과 비교하며 시작했습니다.

## 데이터 객체 만들기

```python
from dataclasses import dataclass

@dataclass
class Incident:
    title: str
    severity: int
    resolved: bool = False
```

C#의 단순 DTO 또는 record와 비슷한 역할을 하지만, Python의 타입 힌트는 기본적으로 런타임 강제가 아니라 개발 도구와 정적 분석을 돕습니다.

## 작은 함수부터 실행하기

```python
def summarize_incident(incident: Incident) -> str:
    status = "해결 완료" if incident.resolved else "처리 중"
    return f"{incident.title}: {status}"
```

관련 소스는 `samples/python/PythonBasics`에 정리했습니다.
