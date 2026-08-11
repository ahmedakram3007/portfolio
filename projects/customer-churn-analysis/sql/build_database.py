"""
Builds churn_analysis.db from the three raw CSVs.
Run from this directory: python build_database.py
"""
import csv
import sqlite3
import os

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_DIR = os.path.join(BASE, "data")
DB_PATH = os.path.join(BASE, "sql", "churn_analysis.db")

if os.path.exists(DB_PATH):
    os.remove(DB_PATH)

conn = sqlite3.connect(DB_PATH)
cur = conn.cursor()

# --- customers (churn) table ---
cur.execute("""
CREATE TABLE customers (
    customer_id TEXT PRIMARY KEY,
    gender TEXT,
    senior_citizen INTEGER,
    partner TEXT,
    dependents TEXT,
    tenure INTEGER,
    phone_service TEXT,
    multiple_lines TEXT,
    internet_service TEXT,
    online_security TEXT,
    online_backup TEXT,
    device_protection TEXT,
    tech_support TEXT,
    streaming_tv TEXT,
    streaming_movies TEXT,
    contract TEXT,
    paperless_billing TEXT,
    payment_method TEXT,
    monthly_charges REAL,
    total_charges REAL,
    churn TEXT
)
""")

with open(os.path.join(DATA_DIR, "Telco-Customer-Churn.csv"), encoding="utf-8") as f:
    reader = csv.DictReader(f)
    rows = []
    for r in reader:
        total_charges = r["TotalCharges"].strip()
        rows.append((
            r["customerID"], r["gender"], int(r["SeniorCitizen"]), r["Partner"], r["Dependents"],
            int(r["tenure"]), r["PhoneService"], r["MultipleLines"], r["InternetService"],
            r["OnlineSecurity"], r["OnlineBackup"], r["DeviceProtection"], r["TechSupport"],
            r["StreamingTV"], r["StreamingMovies"], r["Contract"], r["PaperlessBilling"],
            r["PaymentMethod"], float(r["MonthlyCharges"]),
            float(total_charges) if total_charges else None,
            r["Churn"]
        ))
    cur.executemany("INSERT INTO customers VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", rows)

# --- interactions table ---
cur.execute("""
CREATE TABLE interactions (
    customer_id TEXT,
    interaction_timestamp TEXT,
    channel TEXT,
    interaction_type TEXT,
    outcome TEXT,
    duration TEXT,
    sentiment TEXT,
    agent TEXT
)
""")
with open(os.path.join(DATA_DIR, "Telco-Customer-Interactions.csv"), encoding="utf-8") as f:
    reader = csv.DictReader(f)
    rows = [(
        r["customerID"], r["interactiontimestamp"], r["interactionchannel"],
        r["interaction type"], r["interactionoutcome"], r["interactionduration"],
        r["customersentiment"], r["agent"]
    ) for r in reader]
    cur.executemany("INSERT INTO interactions VALUES (?,?,?,?,?,?,?,?)", rows)

# --- satisfaction table ---
cur.execute("""
CREATE TABLE satisfaction (
    customer_id TEXT,
    gender TEXT,
    satisfaction_score INTEGER
)
""")
with open(os.path.join(DATA_DIR, "Telco-Customer-Satisfaction.csv"), encoding="utf-8") as f:
    reader = csv.DictReader(f)
    rows = [(r["customerID"], r["gender"], int(r["customersatisfaction"]) if r["customersatisfaction"].strip() else None) for r in reader]
    cur.executemany("INSERT INTO satisfaction VALUES (?,?,?)", rows)

conn.commit()

cur.execute("SELECT COUNT(*) FROM customers")
print("customers:", cur.fetchone()[0])
cur.execute("SELECT COUNT(*) FROM interactions")
print("interactions:", cur.fetchone()[0])
cur.execute("SELECT COUNT(*) FROM satisfaction")
print("satisfaction:", cur.fetchone()[0])

conn.close()
print("Database built:", DB_PATH)
