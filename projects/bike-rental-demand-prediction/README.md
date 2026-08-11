# Bike Rental Demand Prediction

Regression model predicting bike rental demand from historical usage and weather data.

## What it does
Builds an end-to-end pipeline: data cleaning, feature preparation, model training and evaluation, using train/test validation to check the model generalises rather than memorising the training data.

Worked on a structured dataset of 10,000 hourly rental records with weather, temperature, and wind-level features. Removed irrelevant fields (e.g. record ID) and filtered to a single city for consistency, then engineered the hourly variable into a binary day/night indicator. Split 80:20 into train/test sets and applied Min-Max normalisation independently to features and labels. Modelled with multivariate Linear Regression, evaluated with Root Mean Squared Error (RMSE) on the normalised [0–1] range.

## Tools
Python, Pandas, Scikit-learn

## Files
- [`ICE-4006_CaseStudy_BikeRegression.ipynb`](ICE-4006_CaseStudy_BikeRegression.ipynb) — the analysis notebook
- [`bike.csv`](bike.csv) — the dataset used
- [`Bike Rental Demand Prediction DS.docx`](Bike%20Rental%20Demand%20Prediction%20DS.docx) — written report

## Notes
Achieved an RMSE of 0.142 on the held-out test set (normalised [0–1] range), trained on ~8,000 records (80%) and evaluated on ~2,000 (20%).
