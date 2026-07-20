# 잠탱이 Dev Lab

Astro 기반 기술 블로그와 학습 코드 저장소입니다.

## 1. 필수 환경

- Node.js 24 이상 권장
- npm
- Git

## 2. 로컬 실행

```bash
npm install
npm run dev
```

브라우저에서 터미널에 표시되는 주소를 엽니다.

## 3. GitHub 설정 전 반드시 수정

`astro.config.mjs`에서 아래 두 값을 수정합니다.

```js
site: 'https://본인아이디.github.io',
base: '/저장소이름',
```

이 프로젝트를 `jamtaeng-dev-lab`이라는 저장소에 올린다면 `base`는 그대로 사용할 수 있습니다.

사용자 사이트 저장소인 `본인아이디.github.io`를 사용하면 `base: '/'`로 변경하세요.

## 4. GitHub Pages 배포

1. 새 GitHub 저장소를 생성합니다.
2. 이 폴더 전체를 push합니다.
3. 저장소 `Settings → Pages`로 이동합니다.
4. Source를 `GitHub Actions`로 선택합니다.
5. main 브랜치에 push하면 자동 배포됩니다.

## 5. 새 글 작성

`src/content/blog` 아래에 Markdown 파일을 추가합니다.

```md
---
title: "글 제목"
description: "한 줄 설명"
publishedAt: 2026-07-20
tags: [CSharp, AI]
draft: false
---

본문을 작성합니다.
```

## 6. 주요 폴더

- `src/content/blog`: 공개 블로그 글
- `src/pages`: 사이트 화면
- `samples`: 글과 연결되는 실행 코드
- `.github/workflows`: GitHub Pages 자동 배포
