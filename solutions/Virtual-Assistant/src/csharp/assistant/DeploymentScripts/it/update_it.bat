@echo off

ECHO Generating it-it LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/it/general.lu -o DeploymentScripts/it --out general.luis -n General
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/it/calendar.lu -o DeploymentScripts/it --out calendar.luis -n Calendar
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/it/email.lu -o DeploymentScripts/it --out email.luis -n Email
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/it/todo.lu -o DeploymentScripts/it --out todo.luis -n ToDo
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/it/pointofinterest.lu -o DeploymentScripts/it --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in CognitiveModels/QnA/it/faq.lu -o DeploymentScripts/it -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/it/dispatch.lu -o DeploymentScripts/it --out dispatch.luis -n Dispatch -i Dispatch -c it-it

@echo on