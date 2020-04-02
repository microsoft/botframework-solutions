Steps
* Install spacy like `pip install -U spacy`. See https://spacy.io/usage#quickstart
* Run server.py
* create_app, update_app and query_app examples:
    - GET id = http://127.0.0.1:5000/create_app
    - POST file as body to http://127.0.0.1:5000/update_app/id
    - GET http://127.0.0.1:5000/query_app/id/text_to_query
    - POST {"query":"text_to_query"} as body to http://127.0.0.1:5000/query_app/id
