-- Customer Churn Analysis — SQL queries
-- Run against churn_analysis.db (SQLite). Three tables: customers, interactions, satisfaction.

-- 1. Overall churn rate
SELECT
    COUNT(*) AS total_customers,
    SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) AS churned,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers;

-- 2. Churn rate by contract type — the single strongest driver in this dataset
SELECT
    contract,
    COUNT(*) AS customers,
    SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) AS churned,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers
GROUP BY contract
ORDER BY churn_rate_pct DESC;

-- 3. Churn rate by tenure bucket — how long customers stay before leaving
SELECT
    CASE
        WHEN tenure <= 6 THEN '0-6 months'
        WHEN tenure <= 12 THEN '7-12 months'
        WHEN tenure <= 24 THEN '13-24 months'
        WHEN tenure <= 48 THEN '25-48 months'
        ELSE '49+ months'
    END AS tenure_bucket,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers
GROUP BY tenure_bucket
ORDER BY MIN(tenure);

-- 4. Churn rate by internet service type
SELECT
    internet_service,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers
GROUP BY internet_service
ORDER BY churn_rate_pct DESC;

-- 5. Churn rate by payment method
SELECT
    payment_method,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers
GROUP BY payment_method
ORDER BY churn_rate_pct DESC;

-- 6. Revenue at risk — monthly recurring revenue currently held by churned customers
SELECT
    ROUND(SUM(CASE WHEN churn = 'Yes' THEN monthly_charges ELSE 0 END), 2) AS monthly_revenue_lost,
    ROUND(SUM(monthly_charges), 2) AS total_monthly_revenue,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN monthly_charges ELSE 0 END) / SUM(monthly_charges), 1) AS pct_revenue_at_risk
FROM customers;

-- 7. Does add-on service adoption reduce churn? (Online Security, Tech Support)
SELECT
    online_security,
    tech_support,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers
WHERE internet_service != 'No'
GROUP BY online_security, tech_support
ORDER BY churn_rate_pct DESC;

-- 8. Satisfaction score vs churn — joins customers to satisfaction
SELECT
    s.satisfaction_score,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN c.churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers c
JOIN satisfaction s ON c.customer_id = s.customer_id
WHERE s.satisfaction_score IS NOT NULL
GROUP BY s.satisfaction_score
ORDER BY s.satisfaction_score;

-- 9. Support interaction sentiment vs churn — joins customers to interactions
-- (a customer can have multiple interactions; this looks at their most negative recorded sentiment)
WITH worst_sentiment AS (
    SELECT
        customer_id,
        CASE
            WHEN MIN(CASE sentiment WHEN 'negative' THEN 1 WHEN 'neutral' THEN 2 WHEN 'positive' THEN 3 END) = 1 THEN 'negative'
            WHEN MIN(CASE sentiment WHEN 'negative' THEN 1 WHEN 'neutral' THEN 2 WHEN 'positive' THEN 3 END) = 2 THEN 'neutral'
            ELSE 'positive'
        END AS worst_sentiment
    FROM interactions
    GROUP BY customer_id
)
SELECT
    ws.worst_sentiment,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN c.churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers c
JOIN worst_sentiment ws ON c.customer_id = ws.customer_id
GROUP BY ws.worst_sentiment
ORDER BY churn_rate_pct DESC;

-- 10. Escalated support interactions vs churn
SELECT
    CASE WHEN i.escalated_count > 0 THEN 'Had an escalated interaction' ELSE 'No escalation' END AS escalation_status,
    COUNT(*) AS customers,
    ROUND(100.0 * SUM(CASE WHEN c.churn = 'Yes' THEN 1 ELSE 0 END) / COUNT(*), 1) AS churn_rate_pct
FROM customers c
LEFT JOIN (
    SELECT customer_id, SUM(CASE WHEN outcome = 'escalated' THEN 1 ELSE 0 END) AS escalated_count
    FROM interactions
    GROUP BY customer_id
) i ON c.customer_id = i.customer_id
GROUP BY escalation_status;
