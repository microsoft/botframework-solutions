@echo off

ECHO Generating it-it LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\it\general.lu -o %0\..\..\..\DeploymentScripts\it --out general.luis -n General
call ludown parse toluis --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\it\calendar.lu -o %0\..\..\..\DeploymentScripts\it --out calendar.luis -n Calendar
call ludown parse toluis --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\it\email.lu -o %0\..\..\..\DeploymentScripts\it --out email.luis -n Email
call ludown parse toluis --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\it\todo.lu -o %0\..\..\..\DeploymentScripts\it --out todo.luis -n ToDo
call ludown parse toluis --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\it\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\it --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in %0\..\..\..\CognitiveModels\QnA\it\faq.lu -o %0\..\..\..\DeploymentScripts\it -n faq.qna
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\it\dispatch.lu -o %0\..\..\..\DeploymentScripts\it --out dispatch.luis -n Dispatch -i Dispatch -c it-it

@echo on