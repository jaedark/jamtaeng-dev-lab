import { defineConfig } from 'astro/config';

// GitHub project pages 기본값입니다.
// 저장소 이름을 바꾸면 base도 함께 바꾸세요.
export default defineConfig({
  site: 'https://jaedark.github.io',
  base: '/jamtaeng-dev-lab',
  output: 'static',
  trailingSlash: 'always'
});
