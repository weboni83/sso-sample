# TodoApi

ASP.NET Core Minimal API로 만든 할 일(Todo) 관리 API입니다. 데이터는 EF Core InMemory DB에 저장되므로 앱을 종료하면 사라집니다.

모든 엔드포인트는 [authentik](../authentik/README.md)이 발급한 access token(JWT)이 있어야 호출할 수 있습니다. 서버 간 호출(client_credentials) 전용입니다.

## 요구 사항

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## 실행

HTTPS로 실행하려면 개발 인증서를 한 번 신뢰해 둡니다.

```bash
dotnet dev-certs https --trust
```

```bash
dotnet run                          # http://localhost:5235
dotnet run --launch-profile https   # https://localhost:7047, http://localhost:5235
```

## API

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/todoitems` | 전체 목록 |
| GET | `/todoitems/complete` | 완료된 항목만 |
| GET | `/todoitems/{id}` | 단건 조회 (없으면 404) |
| POST | `/todoitems` | 추가 (201 + 생성된 항목) |
| PUT | `/todoitems/{id}` | 수정 (204, 없으면 404) |
| DELETE | `/todoitems/{id}` | 삭제 (삭제된 항목 반환, 없으면 404) |

GET은 `todo:read`, POST/PUT/DELETE는 `todo:write` scope가 필요합니다. 토큰이 없거나 유효하지 않으면 `401`, scope가 부족하면 `403`입니다.

## 인증 (authentik, client_credentials)

호출하는 쪽이 가진 "API 키"는 authentik 서비스 계정 `svc-todo-client`의 App Password입니다(`authentik/.env`의 `TODOAPI_CALLER_KEY`). authentik 쪽 구성은 [../authentik/blueprints/todoapi.yaml](../authentik/blueprints/todoapi.yaml)에 있습니다.

1. 키로 access token을 받습니다 (유효시간 5분).

   ```bash
   TOKEN=$(curl -s http://localhost:9000/application/o/token/ \
     -d grant_type=client_credentials -d client_id=todoapi \
     -d username=svc-todo-client --data-urlencode "password=<API 키>" \
     --data-urlencode "scope=todo:read todo:write" \
     | node -e "let d='';process.stdin.on('data',c=>d+=c).on('end',()=>console.log(JSON.parse(d).access_token))")
   ```

   필요한 scope만 요청할 수 있습니다(예: `scope=todo:read` → 조회만 가능한 토큰).

2. Bearer로 호출합니다.

   ```bash
   curl -X POST http://localhost:5235/todoitems \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"name":"walk dog","isComplete":false}'
   ```

TodoApi는 authentik의 공개키(JWKS)로 서명을 검증하고 `iss`, `aud=todoapi`, 만료, `scope`를 확인합니다. 설정은 [appsettings.Development.json](appsettings.Development.json)의 `authentik` 섹션입니다.

| 키 | 값 |
|---|---|
| `authentik:authority` | `http://localhost:9000/application/o/todoapi/` |
| `authentik:audience` | `todoapi` |
| `authentik:requireHttpsMetadata` | `false` (로컬 HTTP authentik이라서. 운영에서는 제거) |

키 폐기·추가는 authentik 관리 화면의 **Directory → Tokens and App passwords**에서 합니다. 호출자별로 서비스 계정과 App Password를 따로 만들면 키를 개별로 폐기할 수 있습니다.

## Docker

.NET 8부터 컨테이너 이미지의 기본 포트는 `8080`입니다.

```bash
docker build -t todoapi .
docker run --rm -p 8080:8080 todoapi   # http://localhost:8080/todoitems
```

> 컨테이너는 `Production` 환경이라 `appsettings.Development.json`의 authentik 설정을 읽지 않습니다. 또 컨테이너 안의 `localhost:9000`은 authentik이 아니므로, 컨테이너로 띄울 때는 authentik과 같은 Docker 네트워크에 두고 토큰의 `iss`와 일치하는 주소로 `authentik__authority` 등을 환경변수로 넘겨야 합니다.

## 사용 패키지

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.0.15
dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore --version 10.0.15
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.15
```
