# Pact Webhook Listener

Aplikacja konsolowa w C# (.NET 9) do nasłuchiwania webhooków z Pact Broker.

## Wymagania

- .NET 9 SDK

## Uruchomienie

```bash
cd PactWebhookListener
dotnet run
```

Aplikacja będzie nasłuchiwać na:
- `http://localhost:6000/webhook` 

## Konfiguracja certyfikatu SSL

Przed pierwszym uruchomieniem skonfiguruj certyfikat deweloperski:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Na macOS/Linux może być potrzebne dodanie certyfikatu do pęku kluczy systemu.
Aplikacja będzie nasłuchiwać na `https://localhost:6001/webhook` (HTTPS) i `http://localhost:6000/webhook` (HTTP fallback)

## Funkcjonalność

Po odebraniu webhooka aplikacja wyświetli w konsoli:
- Consumer (nazwa konsumenta)
- Provider (nazwa providera)
- Pact URL (URL do kontraktu)
- Event (typ zdarzenia)
- Pełne body JSON w oryginalnej postaci

## Przykład wyjścia

```
=== Pact Broker Webhook Received ===
Consumer: ConsumerService
Provider: ProviderService
Pact URL: http://localhost:9292/pacts/provider/ProviderService/consumer/ConsumerService/version/1.0.0
Event: contract_published
Full body:
{
  "consumer": {
    "name": "ConsumerService"
  },
  "provider": {
    "name": "ProviderService"
  },
  "pact_url": "http://localhost:9292/pacts/provider/ProviderService/consumer/ConsumerService/version/1.0.0",
  "event": "contract_published"
}
=====================================
```

## Endpointy

- `POST /webhook` - główny endpoint do odbierania webhooków
- `GET /health` - endpoint sprawdzania stanu aplikacji

### Dostępne zdarzenia:

- `contract_published` - opublikowanie nowego kontraktu
- `contract_changed` - zmiana istniejącego kontraktu
- `contract_content_changed` - zmiana zawartości kontraktu
- `provider_verification_published` - opublikowanie wyniku weryfikacji
- `provider_verification_succeeded` - pomyślna weryfikacja
- `provider_verification_failed` - niepomyślna weryfikacja
