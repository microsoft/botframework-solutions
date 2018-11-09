@echo off

ECHO Generating fr-fr LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/fr/general.lu -o DeploymentScripts/fr --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/fr/calendar.lu -o DeploymentScripts/fr --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/fr/email.lu -o DeploymentScripts/fr --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/fr/todo.lu -o DeploymentScripts/fr --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/fr/pointofinterest.lu -o DeploymentScripts/fr --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/fr/faq.lu -o DeploymentScripts/fr -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/fr/dispatch.lu -o DeploymentScripts/fr --out dispatch.luis -n Dispatch -i Dispatch -c fr-fr

@echo on