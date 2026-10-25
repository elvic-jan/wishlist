## MODIFIED Requirements

### Requirement: Authentication traffic served only over HTTPS

The system SHALL serve all authentication endpoints — registration, login, token refresh, and logout — exclusively over HTTPS in every environment, including local development. The HttpOnly refresh token cookie SHALL be issued with the `Secure` attribute so that browsers refuse to transmit it over plain HTTP.

In local development the API itself terminates TLS and binds no HTTP listener. In deployed environments TLS SHALL terminate at the hosting platform's ingress, which SHALL refuse or redirect plain-HTTP requests from the internet. The container behind it MAY listen on plain HTTP reachable only from that ingress, and the API SHALL derive the request scheme and client IP address from the ingress's `X-Forwarded-Proto` and `X-Forwarded-For` headers so that HTTPS enforcement and per-IP rate limiting act on the real client connection.

#### Scenario: HTTP request to an auth endpoint is refused

- **WHEN** a client sends a request to a registration, login, refresh, or logout endpoint over plain HTTP
- **THEN** the API does not process the credentials or token: in local development the request fails to connect (no HTTP listener is bound), and in deployed environments the platform ingress redirects it to HTTPS without forwarding the credentials or token to the API

#### Scenario: HTTPS request through the platform ingress is accepted

- **WHEN** a client calls an auth endpoint over HTTPS on the deployed origin and the ingress forwards it to the container over plain HTTP with `X-Forwarded-Proto: https`
- **THEN** the API treats the request as HTTPS and processes it normally

#### Scenario: Client IP is taken from the ingress

- **WHEN** two different clients call the password-reset request endpoint through the deployed ingress
- **THEN** the API attributes each request to that client's own IP address (from `X-Forwarded-For`), not to the ingress's address, so the per-IP rate limit counts them separately

#### Scenario: Refresh token cookie cannot leak over HTTP

- **WHEN** the API issues the HttpOnly refresh token cookie after a successful registration, login, or refresh
- **THEN** the cookie carries the `Secure` attribute so the browser only sends it back over HTTPS, and never over an HTTP request to the same host
