@echo off

ECHO Generating es-es LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/es/general.lu -o DeploymentScripts/es --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/es/calendar.lu -o DeploymentScripts/es --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/es/email.lu -o DeploymentScripts/es --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/es/todo.lu -o DeploymentScripts/es --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/es/pointofinterest.lu -o DeploymentScripts/es --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/es/faq.lu -o DeploymentScripts/es -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/es/dispatch.lu -o DeploymentScripts/es --out dispatch.luis -n Dispatch -i Dispatch -c es-es

@echo on