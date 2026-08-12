# 잠탱이 AI Dev Lab

FactoryOps AI를 중심으로 제조 도메인과 Agentic AI를 연결하는 개발 과정을 기록하는 Astro 기반 기술 블로그입니다.

## Current Focus

**FactoryOps AI — Agentic Manufacturing Operations Platform**

제조 장애 데이터를 기반으로 검색, 원인 분석, 대응 추천, 보고서 생성을 자동화하는 AI Agent 플랫폼을 단계적으로 구현합니다.

개발 흐름:

```text
Backend Foundation
→ Keyword Search
→ Vector Search
→ RAG
→ Tool Calling
→ Single Agent
→ Agent Orchestration
→ MCP
→ External System Integration
→ Evaluation / Deployment
```

프로젝트 소스: `jaedark/factoryops-ai`

## Blog Principles

- 직접 구현하고 확인한 내용만 기록합니다.
- 기술을 도입하기 전에 기존 방식의 한계를 먼저 확인합니다.
- 성공 결과뿐 아니라 실패 원인과 트러블슈팅 과정도 남깁니다.
- 각 기술을 왜 사용했는지 설명할 수 있도록 정리합니다.
- 공개 데이터는 synthetic data만 사용합니다.

## Local Development

```bash
npm install
npm run dev
```

## GitHub Pages

`astro.config.mjs`는 GitHub Project Pages 기준으로 설정되어 있습니다.

```js
site: 'https://jaedark.github.io',
base: '/jamtaeng-dev-lab',
```

`main` 브랜치에 push하면 GitHub Actions를 통해 자동 배포됩니다.

## Writing a Post

`src/content/blog` 아래에 Markdown 파일을 추가합니다.

```md
---
title: "글 제목"
description: "한 줄 설명"
publishedAt: 2026-08-12
tags: [Python, FastAPI, FactoryOpsAI]
draft: false
---

본문
```

## Structure

- `src/content/blog`: FactoryOps AI 개발 기록과 기술 글
- `src/pages`: 홈, 개발 기록, 프로젝트, 소개
- `src/layouts`: 공통 레이아웃
- `src/components`: Header / Footer
- `.github/workflows`: GitHub Pages 자동 배포
