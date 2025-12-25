
# Домашняя работа №4. Асинхронное межсервисное взаимодействие
# Gozon (Orders + Payments)

## 1. Краткое описание архитектуры

Система реализована как набор микросервисов на **C# / ASP.NET Core**, с **PostgreSQL** и брокером сообщений **RabbitMQ**, запуск через **Docker Compose**.

Главная идея: **Orders** создаёт заказ и асинхронно инициирует оплату через брокер. **Payments** обрабатывает команду оплаты, списывает деньги и отправляет результат обратно через брокер.  
Доставка сообщений: **at-least-once**, защита от дублей: **Inbox/Outbox + идемпотентность**.

---

## 2. Компоненты

### 2.1. Gateway.Api (`gateway` в docker-compose)

- Единая точка входа для клиента/фронта.
- Проксирует запросы в Orders/Payments.
- Подходит для того, чтобы фронт общался с одним адресом.


### 2.2. OrdersService.Api (`orders`)

-   Хранит заказы и их статусы.
-   При создании заказа:
  -   пишет заказ в БД
  -   пишет команду на оплату в **Outbox** (в той же транзакции)
  -   отдельный воркер публикует сообщение в RabbitMQ.
-   Отдельный consumer слушает события результата оплаты и обновляет статус заказа.

Публичное API:

-   `POST /api/orders` — создать заказ (инициирует оплату асинхронно)
-   `GET /api/orders/{orderId}` — получить заказ по id
-   `GET /api/orders` — список заказов

----------

### 2.3. PaymentsService.Api (`payments`)
-   Хранит аккаунты пользователей и баланс.
-   Обрабатывает команду оплаты из RabbitMQ:
  -   предотвращение повторной обработки входящих сообщений через **Inbox** (по `MessageId`)
  -   списание денег атомарно (без гонок по балансу)
  -   идемпотентность оплаты по `OrderId` (один заказ не списывается дважды)
  -   результат оплаты пишет в **Outbox**
  -   воркер публикует событие результата в RabbitMQ.

Публичное API:
-   `POST /api/payments/accounts` — создать аккаунт
-   `POST /api/payments/accounts/topup` — пополнить баланс
-   `GET /api/payments/accounts/balance` — получить баланс

### 2.4. Frontend (`frontend`)
- Отдельный сервис фронтенда, упакован в Docker.
- Общается с системой через **Gateway** по HTTP (REST).

---

## 3. Асинхронное взаимодействие и гарантии

### 3.1. RabbitMQ
Используется для обмена сообщениями между Orders и Payments.

Типовой поток:
1) Клиент создаёт заказ в Orders
2) Orders кладёт команду **PayOrder** в outbox
3) OutboxPublisher публикует PayOrder в RabbitMQ
4) Payments consumer получает PayOrder, обрабатывает оплату
5) Payments публикует результат (**PaymentSucceeded / PaymentFailed**)
6) Orders consumer получает результат и обновляет заказ

### 3.2. Outbox / Inbox
- **OrdersService**: Outbox (команды на оплату)
- **PaymentsService**: Inbox (защита от повторной обработки входящих сообщений) + Outbox (исходящие события результата)

Это позволяет:
- не терять сообщения при падениях
-  повторять публикацию/обработку
- обеспечивать “effectively exactly once” через идемпотентность.

---


## 4. Хранилище данных

### 4.1. PostgreSQL (`postgres`)

Один контейнер PostgreSQL, **две базы данных**:

-   `orders_db` — база OrdersService
-   `payments_db` — база PaymentsService

Типовые сущности:
-   OrdersService (`orders_db`):

    -   `orders`
    -   `outbox_messages`


-   PaymentsService (`payments_db`):
    -   `accounts`
    -   `inbox_messages`
    -   `payment_transactions` (UNIQUE по `order_id`)
    -   `outbox_messages`
---

## 5. Запуск

### 5.1. Через Docker Compose
В корне репозитория:
```bash
cd deploy
docker compose up --build
