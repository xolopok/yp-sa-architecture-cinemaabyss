# Изучите [README.md](./README.md) файл и структуру проекта.

## Задание 1

### Анализ

**Домен**

Онлайн кинотеатр-аггрегатор.

**Поддомены**

Ключевые:

- Каталог фильмов, Подписки

Вспомогательные:

- Рекомендации, Система лояльности

Прочие:

- Учетные данные, Платежи, Уведомления, Видеофайлы

**Ограниченные контексты**

- Movies (Write/Read, CQRS)
- Subscriptions,
- Recommendations,
- Loyalty
- Identity,
- Payments,
- Video,
- Notifications

_Для упрощения диаграм все сервисы в общем PostgreSQL-кластере. У каждого сервиса своя БД._

**Исходное состояние системы**

- [Контекст (AS IS)](docs/architecture/context.png)
- [Контейнеры (AS IS)](docs/architecture/containers-asis.png)

_Исходил из собственного представления о том как может быть устроена система. Код в репозитории сильно упрощен._

### Промежуточное решение (Strangler Fig)

Новый доменный сервис Movies.

Прокси сервис маршрутизирует трафик следующим образом:
- Не-movies трафик всегда идёт в монолит.
- `GRADUAL_MIGRATION=true`: случайное число 0–99 < `MOVIES_MIGRATION_PERCENT` ? movies-трафик идёт в новый сервис : movies-трафик идёт в монолит.
- `GRADUAL_MIGRATION=false`: movies-трафик идёт в новый сервис.

_По примеру из задания 2._

Между монолитом и новым сервисом стоит ACL-адаптер.

**Промежуточное состояние системы**
- [Контейнеры: миграция (Strangler Fig)](docs/architecture/containers-tobe-migration.png)

### Конечное решение

Монолит и ACL отсутствуют. Вместо прокси сервиса отдельные BFF под каждое приложение. Для асинхронного взаимодействия используется Kafka.

**Конечное состояние системы**

Фронтенды:

- [Контейнеры: Web BFF (TO BE)](docs/architecture/containers-tobe-frontends-web.png)
- [Контейнеры: Mobile BFF (TO BE)](docs/architecture/containers-tobe-frontends-mobile.png)
- [Контейнеры: Smart TV BFF (TO BE)](docs/architecture/containers-tobe-frontends-smarttv.png)
- [Контейнеры: Operator BFF (TO BE)](docs/architecture/containers-tobe-frontends-operator.png)

Сервисы:

- [Контейнеры: сервисы и базы данных (TO BE)](docs/architecture/containers-tobe-services-persistence.png)
- [Контейнеры: сервисы и сообщения (TO BE)](docs/architecture/containers-tobe-services-communication.png)

Прочее:
- [Контейнеры: внешние зависимости (TO BE)](docs/architecture/containers-tobe-ext-deps.png)
- [Контейнеры: observability (TO BE)](docs/architecture/containers-tobe-observability.png)

## Задание 2

_Я поднял версию Node до 22 и поправил скрипты. У меня не получилось запустить тесты как есть: yargs 17.6.2 падает на Node ≥ 20._

_Я добавил bash-скрипты в `tests/scripts/` для быстрого тестирования без Node._

**Сервис Proxy (C#, .NET 10)**

Точка входа: `src/microservices/proxy/Program.cs`.

Маршрутизация:
- Не-movies трафик всегда идёт в монолит.
- `/api/movies` и `/api/movies/*` маршрутизируются в сервис movies:
  - `GRADUAL_MIGRATION=true`: случайное число `0–99 < MOVIES_MIGRATION_PERCENT`, то сервис movies, иначе монолит.
  - `GRADUAL_MIGRATION=false`: весь movies-трафик идёт в movies-сервис.

Тест:
```bash
curl http://localhost:8000/health        # Должен вернуть "Strangler Fig Proxy is healthy"
curl http://localhost:8000/api/movies    # Должен вернуть список фильмов
```

Для проверки постепенного перехода нужно изменить `MOVIES_MIGRATION_PERCENT` в `docker-compose.yml` (0 — весь movies-трафик в монолит, 100 — весь в movies-сервис) и перезапустить `proxy-service`.

**Сервис Events (C#, .NET 10, Kafka)**

Точка входа: `src/microservices/proxy/Program.cs`.

Тест:
```bash
cd tests/postman/
npm run test:local
```

Отчёты в `tests/postman/reports/`.

Скриншоты:

![Тесты](task2_tests.png)
![Топики Kafka](task2_topics.png)

## Задание 3

Команда начала переезд в Kubernetes для лучшего масштабирования и повышения надежности.
Вам, как архитектору, осталось самое сложное:
 - реализовать CI/CD для сборки прокси сервиса
 - реализовать необходимые конфигурационные файлы для переключения трафика.
### CI/CD

В папке .github/workflows доработайте деплой новых сервисов proxy и events в docker-build-push.yml, чтобы api-tests при сборке отрабатывали корректно при отправке коммита в ваш репозиторий.

Нужно доработать
```yaml
on:
  push:
    branches: [ main ]
    paths:
      - 'src/**'
      - '.github/workflows/docker-build-push.yml'
  release:
    types: [published]
```
и добавить необходимые шаги в блок
```yaml
jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v3

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v2

      - name: Log in to the Container registry
        uses: docker/login-action@v2
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

```
Как только сборка отработает и в github registry появятся ваши образы, можно переходить к блоку настройки Kubernetes.
Успешным результатом данного шага является "зеленая" сборка и "зеленые" тесты.
### Proxy в Kubernetes

#### Шаг 1
Для деплоя в Kubernetes необходимо залогиниться в docker registry Github'а.
1. Создайте Personal Access Token (PAT) https://github.com/settings/tokens . Создавайте classic с правом read:packages
2. В src/kubernetes/*.yaml (event-service, monolith, movies-service и proxy-service)  отредактируйте путь до ваших образов
```bash
 spec:
      containers:
      - name: events-service
        image: ghcr.io/ваш логин/имя репозитория/events-service:latest
```
3. Добавьте в секрет src/kubernetes/dockerconfigsecret.yaml в поле
```bash
 .dockerconfigjson: значение в base64 файла ~/.docker/config.json
```

4. Если в `~/.docker/config.json` нет значения для аутентификации
```json
{
        "auths": {
                "ghcr.io": {
                       тут пусто
                }
        }
}
```
то выполните
```bash
docker login ghcr.io
```
и добавьте

```json
 "auth": "имя пользователя:токен в base64"
```

Чтобы получить значение в base64 можно выполнить команду
```bash
 echo -n ваш_логин:ваш_токен | base64
```

После заполнения `config.json`, также прогоните содержимое через base64

```bash
cat .docker/config.json | base64
```

и полученное значение добавляем в

```bash
 .dockerconfigjson: значение в base64 файла ~/.docker/config.json
```

#### Шаг 2

  Доработайте src/kubernetes/event-service.yaml и src/kubernetes/proxy-service.yaml

  - Необходимо создать Deployment и Service
  - Доработайте ingress.yaml, чтобы можно было с помощью тестов проверить создание событий
  - Выполните дальнейшие шаги для поднятия кластера:

  1. Создайте namespace:
  ```bash
  kubectl apply -f src/kubernetes/namespace.yaml
  ```
  2. Создайте секреты и переменные
  ```bash
  kubectl apply -f src/kubernetes/configmap.yaml
  kubectl apply -f src/kubernetes/secret.yaml
  kubectl apply -f src/kubernetes/dockerconfigsecret.yaml
  kubectl apply -f src/kubernetes/postgres-init-configmap.yaml
  ```

  3. Разверните базу данных:
  ```bash
  kubectl apply -f src/kubernetes/postgres.yaml
  ```

  На этом этапе если вызвать команду
  ```bash
  kubectl -n cinemaabyss get pod
  ```
  Вы увидите
  ```bash
  NAME         READY   STATUS
  postgres-0   1/1     Running
  ```

  4. Разверните Kafka:
  ```bash
  kubectl apply -f src/kubernetes/kafka/kafka.yaml
  ```

  Проверьте, теперь должно быть запущено 3 пода, если что-то не так, то посмотрите логи
  ```bash
  kubectl -n cinemaabyss logs имя_пода (например - kafka-0)
  ```

  5. Разверните монолит:
  ```bash
  kubectl apply -f src/kubernetes/monolith.yaml
  ```
  6. Разверните микросервисы:
  ```bash
  kubectl apply -f src/kubernetes/movies-service.yaml
  kubectl apply -f src/kubernetes/events-service.yaml
  ```
  7. Разверните прокси-сервис:
  ```bash
  kubectl apply -f src/kubernetes/proxy-service.yaml
  ```

  После запуска и поднятия подов вывод команды
  ```bash
  kubectl -n cinemaabyss get pod
  ```

  Будет наподобие такого

```bash
  NAME                              READY   STATUS

  events-service-7587c6dfd5-6whzx   1/1     Running

  kafka-0                           1/1     Running

  monolith-8476598495-wmtmw         1/1     Running

  movies-service-6d5697c584-4qfqs   1/1     Running

  postgres-0                        1/1     Running

  proxy-service-577d6c549b-6qfcv    1/1     Running

  zookeeper-0                       1/1     Running
```

  8. Добавим ingress

  - добавьте аддон
  ```bash
  minikube addons enable ingress
  ```
  ```bash
  kubectl apply -f src/kubernetes/ingress.yaml
  ```
   9. Добавьте в /etc/hosts
    ```bash
    127.0.0.1 cinemaabyss.example.com
    ```

  10. Вызовите
  ```bash
  minikube tunnel
  ```
   11. Вызовите `https://cinemaabyss.example.com/api/movies`
    Вы должны увидеть вывод списка фильмов
   Можно поэкспериментировать со значением `MOVIES_MIGRATION_PERCENT` в src/kubernetes/configmap.yaml и убедиться, что вызовы movies уходят полностью в новый сервис

  12. Запустите тесты из папки tests/postman
  ```bash
   npm run test:kubernetes
  ```
  Часть тестов с health-check упадет, но создание событий отработает.
  Откройте логи event-service и сделайте скриншот обработки событий

#### Шаг 3
Добавьте сюда скриншот вывода при вызове `https://cinemaabyss.example.com/api/movies` и скриншот вывода event-service после вызова тестов.
## Задание 4
Для простоты дальнейшего обновления и развертывания вам как архитектуру необходимо также реализовать Helm-чарты для прокси-сервиса и проверить работу

Для этого:
1. Перейдите в директорию helm и отредактируйте файл values.yaml

```yaml
# Proxy service configuration
proxyService:
  enabled: true
  image:
    repository: ghcr.io/db-exp/cinemaabysstest/proxy-service
    tag: latest
    pullPolicy: Always
  replicas: 1
  resources:
    limits:
      cpu: 300m
      memory: 256Mi
    requests:
      cpu: 100m
      memory: 128Mi
  service:
    port: 80
    targetPort: 8000
    type: ClusterIP
```

- Вместо ghcr.io/db-exp/cinemaabysstest/proxy-service напишите свой путь до образа для всех сервисов
- для imagePullSecret проставьте свое значение (скопируйте из конфигурации kubernetes)
  ```yaml
  imagePullSecrets:
      dockerconfigjson: ewoJImF1dGhzIjogewoJCSJnaGNyLmlvIjogewoJCQkiYXV0aCI6ICJaR0l0Wlhod09tZG9jRjl2UTJocVZIa3dhMWhKVDIxWmFVZHJOV2hRUW10aFVXbFZSbTVaTjJRMFNYUjRZMWM9IgoJCX0KCX0sCgkiY3JlZHNTdG9yZSI6ICJkZXNrdG9wIiwKCSJjdXJyZW50Q29udGV4dCI6ICJkZXNrdG9wLWxpbnV4IiwKCSJwbHVnaW5zIjogewoJCSIteC1jbGktaGludHMiOiB7CgkJCSJlbmFibGVkIjogInRydWUiCgkJfQoJfSwKCSJmZWF0dXJlcyI6IHsKCQkiaG9va3MiOiAidHJ1ZSIKCX0KfQ==
  ```

2. В папке ./templates/services заполните шаблоны для proxy-service.yaml и events-service.yaml (опирайтесь на свою kubernetes конфигурацию - смысл helm'а сделать шаблоны для быстрого обновления и установки)

```yaml
template:
    metadata:
      labels:
        app: proxy-service
    spec:
      containers:
       Тут ваша конфигурация
```

3. Проверьте установку
Сначала удалим установку руками

```bash
kubectl delete all --all -n cinemaabyss
kubectl delete namespace cinemaabyss
```
Запустите
```bash
helm install cinemaabyss ./src/kubernetes/helm --namespace cinemaabyss --create-namespace
```
Если в процессе будет ошибка
```text
[2025-04-08 21:43:38,780] ERROR Fatal error during KafkaServer startup. Prepare to shutdown (kafka.server.KafkaServer)
kafka.common.InconsistentClusterIdException: The Cluster ID OkOjGPrdRimp8nkFohYkCw doesn't match stored clusterId Some(sbkcoiSiQV2h_mQpwy05zQ) in meta.properties. The broker is trying to join the wrong cluster. Configured zookeeper.connect may be wrong.
```

Проверьте развертывание:
```bash
kubectl get pods -n cinemaabyss
minikube tunnel
```

Потом вызовите
`https://cinemaabyss.example.com/api/movies`
и приложите скриншот развертывания Helm и вывода `https://cinemaabyss.example.com/api/movies`

### Удаляем все

```bash
kubectl delete all --all -n cinemaabyss
kubectl delete namespace cinemaabyss
```
