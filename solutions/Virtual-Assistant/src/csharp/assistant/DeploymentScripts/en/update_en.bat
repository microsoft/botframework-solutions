@echo off

ECHO Generating en-us LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in CognitiveModels/LUIS/en/general.lu -o DeploymentScripts/en -n General --out general.luis
call ludown parse toluis --in ../skills/calendarskill/CognitiveModels/LUIS/en/calendar.lu -o DeploymentScripts/en -n Calendar --out calendar.luis
call ludown parse toluis --in ../skills/emailskill/CognitiveModels/LUIS/en/email.lu -o DeploymentScripts/en -n Email --out email.luis -n Email
call ludown parse toluis --in ../skills/pointofinterestskill/CognitiveModels/LUIS/en/pointofinterest.lu -o DeploymentScripts/en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../skills/todoskill/CognitiveModels/LUIS/en/todo.lu -o DeploymentScripts/en -n ToDo --out todo.luis
call ludown parse toqna --in CognitiveModels/QnA/en/faq.lu -o DeploymentScripts/en -n faq.qna
call ludown parse toluis --in CognitiveModels/LUIS/en/dispatch.lu -o DeploymentScripts/en --out dispatch.luis -n Dispatch -i Dispatch

@echo on