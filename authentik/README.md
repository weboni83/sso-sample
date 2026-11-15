# authentik (로컬 개발용)

공식 [docker-compose.yml](https://goauthentik.io/docker-compose.yml) (2026.8.5)을 그대로 사용합니다.

## 최초 실행

```bash
cp .env.example .env   # PG_PASS, AUTHENTIK_SECRET_KEY 에 랜덤 값 입력
docker compose up -d
```

첫 기동 시 DB 마이그레이션과 blueprint 적용에 몇 분 걸립니다. 끝나면 관리자(`akadmin`) 비밀번호를 설정합니다.

- 초기 설정: **http://localhost:9000/ (루트)** 로 접속하면 설정 화면으로 이동합니다.
  `/if/flow/initial-setup/`으로 바로 들어가면 "Access the authentik setup by navigating to http://localhost:9000/" 로 거부됩니다(2026.x부터 루트에서 시작한 세션만 허용). 이미 거부 화면을 본 브라우저는 시크릿 창을 쓰거나 localhost 쿠키를 지운 뒤 루트로 다시 접속하세요.
- 관리 화면: http://localhost:9000/if/admin/
- HTTPS(자체 서명): https://localhost:9443

## 자주 쓰는 명령

```bash
docker compose ps
docker compose logs -f server worker
docker compose down        # 중지 (데이터 유지)
docker compose down -v     # DB 볼륨까지 삭제 (초기화)
```

## Blueprint (선언형 설정)

[blueprints/](blueprints/) 폴더는 [docker-compose.override.yml](docker-compose.override.yml)로 컨테이너의 `/blueprints/custom`에 마운트되어 기동 시 자동 적용됩니다. 비밀값은 `!Env`로 `.env`에서 읽습니다.

| 파일 | 만드는 것 |
|---|---|
| [sociallogins.yaml](blueprints/sociallogins.yaml) | OAuth2/OIDC Provider + Application `sociallogins`, 테스트 사용자 `demo` |

Windows 바인드 마운트에서는 파일 변경 감지가 잘 안 되므로, 수정 후에는 직접 적용합니다.

```bash
# Git Bash에서는 경로 변환을 막아야 함 (MSYS_NO_PATHCONV=1)
docker compose exec worker ak apply_blueprint custom/sociallogins.yaml
```

> blueprint로 OAuth2 Provider를 만들 때는 `grant_types`를 반드시 적어야 합니다. 비어 있으면 authorize 요청이 `invalid_request` ("Invalid grant_type for provider")로 거절됩니다.

## SocialLogins-authentik 연동

| 항목 | 값 |
|---|---|
| authority | `http://localhost:9000/application/o/sociallogins/` |
| client id | `sociallogins` |
| client secret | `.env`의 `SOCIALLOGINS_CLIENT_SECRET` |
| 로그인 사용자 | `demo` / `.env`의 `DEMO_USER_PASSWORD` |

앱 설정 (저장소 루트에서, Git Bash):

```bash
P=SocialLogins-authentik/SocialLogins
dotnet user-secrets set authentik:authority "http://localhost:9000/application/o/sociallogins/" --project $P
dotnet user-secrets set authentik:clientId sociallogins --project $P
dotnet user-secrets set authentik:clientSecret "$(grep '^SOCIALLOGINS_CLIENT_SECRET=' authentik/.env | cut -d= -f2-)" --project $P
dotnet user-secrets set authentik:requireHttpsMetadata false --project $P   # authentik을 HTTP로 쓰기 때문
dotnet run --project $P   # https://localhost:5001
```

앱 코드 쪽 설명은 [../SocialLogins-authentik/readme.md](../SocialLogins-authentik/readme.md) 참고.
