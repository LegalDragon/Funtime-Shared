// Runtime configuration - auto-detects environment, no editing needed per deploy
// This file is loaded before the app starts via index.html
//
// localhost → dev backend at https://localhost:5001
// anything else → same-origin /api (production)
//
(function () {
  var isDev = window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1";

  window.__FUNTIME_CONFIG__ = {
    API_URL: isDev ? "https://localhost:5001" : "/api",
    STRIPE_PUBLISHABLE_KEY: isDev
      ? "pk_test_51Sjh30GYp7v2V1rfaXTN17VZ8S2XEy41j6xCLhm8RUtSjRn9rsCieuqGPfncNeoIyygAX8Gh7OmIpd0zJLx28UXY00unCayIRo"
      : "pk_test_51Sjh30GYp7v2V1rfaXTN17VZ8S2XEy41j6xCLhm8RUtSjRn9rsCieuqGPfncNeoIyygAX8Gh7OmIpd0zJLx28UXY00unCayIRo"
  };
})();
