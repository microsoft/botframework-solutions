@echo off

ECHO Generating es-es LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\es\general.lu -o %0\..\..\..\DeploymentScripts\es --out general.luis -n General
call ludown parse toluis --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\es\calendar.lu -o %0\..\..\..\DeploymentScripts\es --out calendar.luis -n Calendar
call ludown parse toluis --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\es\email.lu -o %0\..\..\..\DeploymentScripts\es --out email.luis -n Email
call ludown parse toluis --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\es\todo.lu -o %0\..\..\..\DeploymentScripts\es --out todo.luis -n ToDo
call ludown parse toluis --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\es\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\es --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\es\faq.lu -o %0\..\..\..\DeploymentScripts\es -n faq.qna
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\es\dispatch.lu -o %0\..\..\..\DeploymentScripts\es --out dispatch.luis -n Dispatch -i Dispatch -c es-es

@echo on