# BrightHrKata: Checkout

A supermarket checkout pricing kata, originally developed as a standalone coding exercise for BrightHR. In this project it serves as the **primary target application** for the AutoRepair pipeline.

---

## What It Does

The `Checkout` library models a supermarket till that:

1. Maintains a **stock catalogue** of products (A to D) with unit prices.
2. Accepts scanned items via `Scan(char sku)`.
3. Calculates a running total via `GetTotalCost()`, applying **special prices** (e.g., "3 A's for 130" instead of 3 x 50).

## How AutoRepair Uses It

The tracing pipeline (Step 1) instruments every `*.cs` file in `Checkout/`, then runs the `Checkout.Tests` MSTest suite as the driver. This produces a rich execution trace covering normal pricing, special-price logic, and edge cases, all without modifying the test project itself.

```text
generate_trace.ps1
  --> instruments Checkout/*.cs
  --> runs Checkout.Tests (MSTest)
  --> collects trace_logs.jsonl
```

## Pricing Rules

| SKU | Unit Price | Special Price |
| :-: | :---: | :--- |
| A | 50 | 3 for 130 |
| B | 30 | 2 for 45 |
| C | 20 | None |
| D | 15 | None |

(Defined in `StockCatalogue.cs` and `SpecialPrices.cs`.)

## Project Layout

```text
BrightHrKata/
+-- Checkout/                # Library under test
|   +-- Checkout.cs          # Main checkout class (Scan, GetTotalCost)
|   +-- PricingEngine.cs     # Applies normal + special pricing
|   +-- Basket.cs            # Holds scanned items
|   +-- StockCatalogue.cs    # Product definitions (SKU, name, price)
|   +-- SpecialPrices.cs     # Discount rules
|   +-- Interfaces/          # Abstractions (ICheckout, IBasket, etc.)
+-- Checkout.Tests/          # MSTest suite
|   +-- CheckoutTests.cs     # End-to-end scan & total tests
|   +-- PricingEngineTests.cs # Unit tests for pricing logic
+-- test-cases.json          # Reference test data
+-- README.md                # You are here
```
