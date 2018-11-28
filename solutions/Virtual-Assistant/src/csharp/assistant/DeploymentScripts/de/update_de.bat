@echo off

ECHO Generating de-de LUIS and QnA Maker models from .lu files ..
call ludown parse toluis -c de-de --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\de\calendar.lu -o %0\..\..\..\DeploymentScripts\de --out calendar.luis -n Calendar
call ludown parse toluis -c de-de --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\de\email.lu -o %0\..\..\..\DeploymentScripts\de --out email.luis -n Email
call ludown parse toluis -c de-de --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\de\todo.lu -o %0\..\..\..\DeploymentScripts\de --out todo.luis -n ToDo
call ludown parse toluis -c de-de --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\de\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\de --out pointofinterest.luis -n PointOfInterest
call ludown parse toluis -c de-de --in %0\..\..\..\CognitiveModels\LUIS\de\general.lu -o %0\..\..\..\DeploymentScripts\de --out general.luis -n General
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\de\faq.lu -o %0\..\..\..\DeploymentScripts\de -n faq.qna 
call ludown parse toluis -c de-de --in %0\..\..\..\CognitiveModels\LUIS\de\dispatch.lu -o %0\..\..\..\DeploymentScripts\de --out dispatch.luis -n Dispatch -i Dispatch
@echo on