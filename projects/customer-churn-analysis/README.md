# Customer Churn Analysis

SQL analysis and a predictive model on a 7,043-customer telecom dataset, presented as a live dashboard: **https://ahmedakram3007.github.io/portfolio/churn-dashboard.html**

## What it does
Joins three tables (customer profile/billing, support interaction logs, satisfaction scores) to identify which customers are likely to churn and why, then builds a classification model to predict it. Findings and the model are presented as an interactive dashboard rather than a static report.

## Tools
SQL (SQLite), Python (Pandas, Scikit-learn), matplotlib

## Key findings
- **Contract type is the dominant driver**: 42.7% churn for month-to-month customers vs. 2.8% for two-year contracts.
- **New customers are highest-risk**: 52.9% churn in the first 6 months vs. 9.5% after 49+ months.
- **Add-on services matter**: customers with both Online Security and Tech Support churn at 9.0% vs. 49.0% for customers with neither — the clearest actionable lever in the data.
- **Satisfaction score and support-interaction sentiment show almost no correlation with churn** (flat at ~25–28% regardless) — a genuinely useful negative result suggesting this business's churn problem is structural (contract/price/tenure), not a service-quality problem.
- A class-balanced Random Forest and a Logistic Regression both reach ~0.84 ROC-AUC against a 0.734 baseline; feature importances corroborate the SQL findings (tenure, contract length, and charges dominate).

## Files
- [`sql/analysis_queries.sql`](sql/analysis_queries.sql) — 10 business-facing SQL queries (churn rate by contract/tenure/internet/payment, revenue at risk, add-on impact, satisfaction/sentiment joins)
- [`sql/build_database.py`](sql/build_database.py) — loads the three raw CSVs into a SQLite database
- [`notebook/churn_eda_and_model.ipynb`](notebook/churn_eda_and_model.ipynb) — exploratory analysis, Logistic Regression + Random Forest models, ROC curve, feature importances (executed, with real outputs)
- [`data/`](data) — the three raw CSVs (customers, interactions, satisfaction)

## Notes
Data quality: 11 customers had a blank `TotalCharges` field — all had `tenure == 0` (brand-new customers not yet billed), so these were dropped for modelling rather than imputed. Modelling used a plain train/test split with no time dimension in the data, so this doesn't account for concept drift over time — a real deployment would need to monitor for that.
