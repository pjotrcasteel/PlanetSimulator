"""Compare independent host exports, allowing documented floating point differences."""
import csv
import math
import sys

with open(sys.argv[1], newline="", encoding="utf-8") as source:
    browser = list(csv.DictReader(source))
with open(sys.argv[2], newline="", encoding="utf-8") as source:
    desktop = list(csv.DictReader(source))
if not browser or len(browser) != len(desktop) or browser[0].keys() != desktop[0].keys():
    raise SystemExit("Experiment exports have different sample counts or columns.")
for first, second in zip(browser, desktop):
    for column in first:
        a, b = float(first[column]), float(second[column])
        absolute_tolerance = 0.02 if column == "budget_error_J_m2" else 1e-8
        if column in ("water_mass_error_kg", "water_budget_error_kg"):
            absolute_tolerance = max(1, float(first["total_water_kg"])) * 1e-12
        if column in ("carbon_budget_error_kg", "oxygen_budget_error_kg", "nitrogen_budget_error_kg"):
            absolute_tolerance = max(1, float(first["total_dry_gas_kg"])) * 1e-12
        if column.endswith("error_kg") and max(abs(a), abs(b)) > absolute_tolerance:
            raise SystemExit(f"Conservation tolerance exceeded: {column}: {a}, {b}")
        if not math.isfinite(a) or not math.isfinite(b) or not math.isclose(a, b, rel_tol=1e-10, abs_tol=absolute_tolerance):
            raise SystemExit(f"Host mismatch at day {first.get('day', first.get('cell'))}, {column}: {a} vs {b}")
print(f"Browser and desktop agree: {len(browser)} samples; relative 1e-10, absolute 1e-8; energy residual 0.02 J/m2; mass residual 1e-12 of inventory.")
