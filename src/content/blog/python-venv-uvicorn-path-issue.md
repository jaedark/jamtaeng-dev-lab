---
title: "Python 가상환경이 꼬여 FastAPI가 다른 Uvicorn을 실행했던 문제"
description: "SQLAlchemy를 설치했는데도 ModuleNotFoundError가 발생했던 원인을 Python 가상환경과 Uvicorn 실행 경로 관점에서 정리했습니다."
publishedAt: 2026-08-12
tags: [Python, FastAPI, Uvicorn, SQLAlchemy, Troubleshooting, FactoryOpsAI]
draft: false
---

FactoryOps AI 프로젝트를 시작하면서 Python과 FastAPI로 백엔드를 구성하고 있습니다.

첫날 목표는 단순했습니다. FastAPI 서버를 띄우고, 제조 장애 데이터를 저장하기 위해 SQLAlchemy와 SQLite를 연결하는 것이었습니다.

그런데 SQLAlchemy를 설치했는데도 계속 아래 오류가 발생했습니다.

```text
ModuleNotFoundError: No module named 'sqlalchemy'
```

처음에는 당연히 패키지 설치 문제라고 생각했습니다.

```bash
pip install sqlalchemy
```

그런데 다시 실행해도 똑같았습니다. 패키지는 설치되어 있는데 애플리케이션에서는 찾지 못했습니다.

결론부터 말하면 문제는 **SQLAlchemy가 아니라 서버를 실행하고 있던 Python 환경**이었습니다.

## 프로젝트 가상환경은 따로 있었는데

FactoryOps AI 프로젝트는 다음 경로에서 개발하고 있습니다.

```text
C:\Projects\FactoryOpsAI
```

프로젝트 전용 가상환경도 만들었습니다.

```text
C:\Projects\FactoryOpsAI\.venv
```

가상환경을 활성화하고 필요한 패키지를 설치했기 때문에 당연히 이 환경에서 서버가 실행되고 있다고 생각했습니다.

하지만 에러 로그를 자세히 보니 프로젝트의 `.venv`가 아닌, 예전에 사용했던 다른 프로그램의 Python 가상환경 경로가 보였습니다.

즉 이런 상태였습니다.

```text
FactoryOpsAI .venv
  └─ SQLAlchemy 설치됨

다른 Python venv
  └─ Uvicorn 실행
  └─ SQLAlchemy 없음
```

SQLAlchemy는 제대로 설치되어 있었습니다. 다만 **서버가 다른 Python으로 실행되고 있었던 것**입니다.

## Windows에는 Python이 하나만 있는 게 아니었다

문제를 확인하면서 제 PC에 여러 Python 실행 환경이 있다는 것도 다시 확인했습니다.

예를 들면 이런 식입니다.

```text
C:\Python314\python.exe
C:\Users\...\anaconda3\python.exe
C:\Users\...\Microsoft\WindowsApps\python.exe
C:\Projects\FactoryOpsAI\.venv\Scripts\python.exe
```

여기에 각 프로그램이 자체적으로 만든 가상환경까지 섞이면 실행 경로가 더 복잡해질 수 있습니다.

Windows에서는 다음 명령으로 현재 검색되는 Python 경로를 확인할 수 있습니다.

```bat
where python
```

Uvicorn도 마찬가지입니다.

```bat
where uvicorn
```

이 명령을 확인하면서 제가 생각했던 Python 환경과 실제 명령이 참조하는 실행 파일이 다를 수 있다는 것을 알게 됐습니다.

## 해결: `uvicorn` 대신 `python -m uvicorn`

프로젝트 폴더로 이동하고 가상환경을 다시 활성화했습니다.

```bat
cd C:\Projects\FactoryOpsAI
.venv\Scripts\activate
```

그리고 서버 실행 명령을 바꿨습니다.

기존에는 이렇게 실행했습니다.

```bat
uvicorn backend.main:app --reload
```

이후에는 다음처럼 실행했습니다.

```bat
python -m uvicorn backend.main:app --reload
```

이렇게 실행하자 프로젝트의 가상환경 Python에서 Uvicorn 모듈이 실행됐고, SQLAlchemy도 정상적으로 import됐습니다.

## 왜 `python -m uvicorn`이 도움이 됐을까?

두 명령 모두 결국 Uvicorn을 실행합니다.

하지만 실행 파일을 찾는 방식이 다릅니다.

```text
uvicorn ...
→ PATH에서 uvicorn 실행 파일 탐색

python -m uvicorn ...
→ 현재 선택된 Python에서 uvicorn 모듈 실행
```

프로젝트 가상환경이 제대로 활성화되어 있다면 두 번째 방식은 다음 흐름이 됩니다.

```text
FactoryOpsAI\.venv\Scripts\python.exe
        ↓
     uvicorn
        ↓
 FastAPI Application
```

그래서 지금은 FactoryOps AI 프로젝트를 실행할 때 `python -m uvicorn` 방식을 사용하고 있습니다.

## 패키지 오류가 나면 설치부터 하지 말자

이번에 가장 크게 배운 부분입니다.

예전 같으면 `ModuleNotFoundError`를 보면 바로 설치 명령부터 실행했을 것 같습니다.

```text
ModuleNotFoundError
→ 패키지가 없나?
→ pip install
```

하지만 패키지가 이미 설치되어 있다면 다음 질문이 먼저여야 했습니다.

> 지금 이 애플리케이션은 어떤 Python으로 실행되고 있는가?

그래서 비슷한 문제가 생기면 앞으로는 다음 순서로 확인하려고 합니다.

1. 프로젝트 가상환경이 활성화되어 있는지 확인
2. `where python`
3. `python --version`
4. `python -m pip list`
5. `where uvicorn`
6. traceback에 표시되는 실제 Python 경로 확인

특히 `pip` 역시 단독으로 실행하기보다 아래처럼 사용하면 현재 Python과 연결된 pip를 명확하게 확인할 수 있습니다.

```bat
python -m pip list
```

## FactoryOps AI는 이렇게 시작했다

이번 문제는 제가 만들고 있는 FactoryOps AI 프로젝트를 시작한 첫날에 발생했습니다.

FactoryOps AI는 제조 현장의 장애 이력을 바탕으로 장애 검색, 원인 분석, 대응 방법 추천과 보고서 생성까지 확장해보는 프로젝트입니다.

현재 백엔드는 다음 구조까지 구현했습니다.

```text
Client
  ↓
FastAPI Router
  ↓
Service
  ↓
Repository
  ↓
SQLAlchemy
  ↓
SQLite
```

앞으로는 단순히 AI 라이브러리를 하나씩 붙이는 방식이 아니라, 기존 방식의 한계를 직접 확인하면서 확장할 계획입니다.

```text
Keyword Search
→ Vector Search
→ RAG
→ Tool Calling
→ AI Agent
→ Agent Orchestration
→ MCP
```

예를 들어 Keyword Search를 먼저 구현한 뒤 의미가 비슷하지만 단어가 다른 장애를 찾지 못하는 문제를 확인하고, 그 다음 Vector Search를 도입하는 식입니다.

기술 이름을 많이 써보는 것보다 **왜 그 기술이 필요했는지 설명할 수 있는 프로젝트**를 만드는 것이 이번 프로젝트의 목표입니다.

## 마무리

이번 오류는 코드의 문제가 아니었습니다.

패키지도 정상적으로 설치되어 있었습니다.

문제는 제가 생각하고 있던 Python 환경과 실제 서버를 실행한 Python 환경이 달랐다는 점이었습니다.

Python에서 이상한 모듈 오류를 만났다면 패키지를 다시 설치하기 전에 한 번쯤 확인해볼 만합니다.

```bat
where python
where uvicorn
python -m pip list
```

그리고 프로젝트 가상환경을 확실하게 사용하고 싶다면 서버 실행도 다음처럼 해볼 수 있습니다.

```bat
python -m uvicorn backend.main:app --reload
```

작은 문제였지만 Python 개발 환경을 이해하는 데는 꽤 좋은 첫 삽질이었습니다.

---

FactoryOps AI 소스 코드는 GitHub의 `jaedark/factoryops-ai` 저장소에서 함께 정리하고 있습니다.
