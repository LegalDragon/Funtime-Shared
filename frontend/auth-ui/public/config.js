// Runtime configuration - edit this file per environment without rebuilding
// This file is loaded before the app starts via index.html
//
// For local development with separate backend: use "https://localhost:5001"
// For production (same origin): use "/api"
//
window.__FUNTIME_CONFIG__ = {
  // API URL - use "/api" for same-origin deployment, full URL for cross-origin
  API_URL: "https://localhost:5001",
  //API_URL: "/api",

  // Stripe publishable key (test key for development, live key for production)
  STRIPE_PUBLISHABLE_KEY: "pk_test_51Sjh30GYp7v2V1rfaXTN17VZ8S2XEy41j6xCLhm8RUtSjRn9rsCieuqGPfncNeoIyygAX8Gh7OmIpd0zJLx28UXY00unCayIRo"
};
