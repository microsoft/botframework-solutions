@echo off

ECHO Generating de-de LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/de/general.lu -o DeploymentScripts/de --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/de/calendar.lu -o DeploymentScripts/de --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/de/email.lu -o DeploymentScripts/de --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/de/todo.lu -o DeploymentScripts/de --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/de/pointofinterest.lu -o DeploymentScripts/de --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/de/faq.lu -o DeploymentScripts/de -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/de/dispatch.lu -o DeploymentScripts/de --out dispatch.luis -n Dispatch -i Dispatch -c de-de

@echo on