# authentik (로컬 개발용)

공식 [docker-compose.yml](https://goauthentik.io/docker-compose.yml) (2026.8.5)을 그대로 사용합니다.

## 최초 실행

```bash
cp .env.example .env   # PG_PASS, AUTHENTIK_SECRET_KEY 에 랜덤 값 입력
docker compose up -d
```

첫 기동 시 DB 마이그레이션과 blueprint 적용에 몇 분 걸립니다. 끝나면 아래 주소에서 관리자(`akadmin`) 비밀번호를 설정합니다.

- 초기 설정: http://localhost:9000/if/flow/initial-setup/ (끝 `/` 필수)
- 관리 화면: http://localhost:9000/if/admin/
- HTTPS(자체 서명): https://localhost:9443

## 자주 쓰는 명령

```bash
docker compose ps
docker compose logs -f server worker
docker compose down        # 중지 (데이터 유지)
docker compose down -v     # DB 볼륨까지 삭제 (초기화)
```

## 앱 연동 (OIDC)

authority는 `http://localhost:9000/application/o/<app-slug>/` 형식이며, HTTP로 연동할 때는 앱 쪽에서 `RequireHttpsMetadata=false`가 필요합니다. 자세한 설정은 [../SocialLogins-authentik/readme.md](../SocialLogins-authentik/readme.md) 참고.
