@echo off

ECHO Generating en-us LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/en/general.lu -o DeploymentScripts/en -n General --out general.luis
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/en/calendar.lu -o DeploymentScripts/en -n Calendar --out calendar.luis
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/en/email.lu -o DeploymentScripts/en -n Email --out email.luis -n Email
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/en/pointofinterest.lu -o DeploymentScripts/en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/en/todo.lu -o DeploymentScripts/en -n ToDo --out todo.luis
call ludown parse toqna --in CognitiveModels/QnA/en/faq.lu -o DeploymentScripts/en -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/en/dispatch.lu -o DeploymentScripts/en --out dispatch.luis -n Dispatch -i Dispatch

ECHO Generating de-de LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/de/general.lu -o DeploymentScripts/de --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/de/calendar.lu -o DeploymentScripts/de --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/de/email.lu -o DeploymentScripts/de --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/de/todo.lu -o DeploymentScripts/de --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/de/pointofinterest.lu -o DeploymentScripts/de --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/de/faq.lu -o DeploymentScripts/de -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/de/dispatch.lu -o DeploymentScripts/de --out dispatch.luis -n Dispatch -i Dispatch -c de-de

ECHO Generating es-es LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/es/general.lu -o DeploymentScripts/es --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/es/calendar.lu -o DeploymentScripts/es --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/es/email.lu -o DeploymentScripts/es --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/es/todo.lu -o DeploymentScripts/es --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/es/pointofinterest.lu -o DeploymentScripts/es --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/es/faq.lu -o DeploymentScripts/es -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/es/dispatch.lu -o DeploymentScripts/es --out dispatch.luis -n Dispatch -i Dispatch -c es-es

ECHO Generating fr-fr LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/fr/general.lu -o DeploymentScripts/fr --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/fr/calendar.lu -o DeploymentScripts/fr --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/fr/email.lu -o DeploymentScripts/fr --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/fr/todo.lu -o DeploymentScripts/fr --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/fr/pointofinterest.lu -o DeploymentScripts/fr --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/fr/faq.lu -o DeploymentScripts/fr -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/fr/dispatch.lu -o DeploymentScripts/fr --out dispatch.luis -n Dispatch -i Dispatch -c fr-fr

ECHO Generating it-it LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/it/general.lu -o DeploymentScripts/it --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/it/calendar.lu -o DeploymentScripts/it --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/it/email.lu -o DeploymentScripts/it --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/it/todo.lu -o DeploymentScripts/it --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/it/pointofinterest.lu -o DeploymentScripts/it --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/it/faq.lu -o DeploymentScripts/it -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/it/dispatch.lu -o DeploymentScripts/it --out dispatch.luis -n Dispatch -i Dispatch -c it-it

ECHO Generating zh-hans LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/zh-hans/general.lu -o DeploymentScripts/zh-hans --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/zh-hans/calendar.lu -o DeploymentScripts/zh-hans --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/zh-hans/email.lu -o DeploymentScripts/zh-hans --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/zh-hans/todo.lu -o DeploymentScripts/zh-hans --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/zh-hans/pointofinterest.lu -o DeploymentScripts/zh-hans --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/zh-hans/faq.lu -o DeploymentScripts/zh-hans -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/zh-hans/dispatch.lu -o DeploymentScripts/zh-hans --out dispatch.luis -n Dispatch -i Dispatch -c zh-cn

@echo on