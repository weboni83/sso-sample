# TodoApi

ASP.NET Core Minimal API로 만든 할 일(Todo) 관리 API입니다. 데이터는 EF Core InMemory DB에 저장되므로 앱을 종료하면 사라집니다.

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

요청 본문 예시:

```bash
curl -X POST http://localhost:5235/todoitems \
  -H "Content-Type: application/json" \
  -d '{"name":"walk dog","isComplete":false}'
```

## Docker

.NET 8부터 컨테이너 이미지의 기본 포트는 `8080`입니다.

```bash
docker build -t todoapi .
docker run --rm -p 8080:8080 todoapi   # http://localhost:8080/todoitems
```

## 사용 패키지

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.0.15
dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore --version 10.0.15
```
