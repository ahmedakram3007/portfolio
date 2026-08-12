# F1 Events Corpus — NLP Analysis

NLP analysis of a text corpus built from Wikipedia articles on Formula 1 events, covering multiple seasons.

## What it does
Scrapes and cleans the source text, then applies NLP techniques (entity recognition, text analysis) to surface patterns across events.

## Tools
Python

## Files
- [`NLP mini project assignment.docx`](NLP%20mini%20project%20assignment.docx) — the full write-up: corpus creation methodology, NLP techniques, results/analysis, discussion, and full source code appendix
- [`NLP_mini_project_SC.ipynb`](NLP_mini_project_SC.ipynb) — corpus creation (scraping Wikipedia F1 season pages with BeautifulSoup, sentence tokenising with NLTK) plus the NLP analysis stage
- [`f1_notable_sentences_corpus.txt`](f1_notable_sentences_corpus.txt) — the actual generated corpus: keyword-filtered sentences grouped by F1 season (1950–present), the real output of the scraping script above

Scraped and keyword-filtered lead sections from 70 of 76 F1 season pages on Wikipedia (6 skipped/errored), producing a corpus of 1,235 notable-event sentences. Applied NLTK for sentence tokenisation and POS tagging, and spaCy (`en_core_web_sm`) for Named Entity Recognition, then analysed the results for vocabulary structure and entity frequency patterns.

## Notes
Main challenge was scraping reliability against Wikipedia's varying HTML structure (particularly identifying the correct season-list table via headers). Keyword-based filtering was effective for focus but may miss relevant sentences or include ones where the keyword isn't central. spaCy's NER showed reasonable performance but some misclassification between PERSON/ORG/GPE in the F1 context. Future work: a supervised sentence classifier instead of keyword filtering, and expanding into topic modelling or relation extraction (e.g. driver–team links).
