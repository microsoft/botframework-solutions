@echo off

ECHO Generating en-us LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\en\general.lu -o %0\..\..\..\DeploymentScripts\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\..\..\skills\calendarskill\CognitiveModels\LUIS\en\calendar.lu -o %0\..\..\..\DeploymentScripts\en -n Calendar --out calendar.luis
call ludown parse toluis --in %0\..\..\..\..\skills\emailskill\CognitiveModels\LUIS\en\email.lu -o %0\..\..\..\DeploymentScripts\en -n Email --out email.luis -n Email
call ludown parse toluis --in %0\..\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\en\pointofinterest.lu -o %0\..\..\..\DeploymentScripts\en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in %0\..\..\..\..\skills\todoskill\CognitiveModels\LUIS\en\todo.lu -o %0\..\..\..\DeploymentScripts\en -n ToDo --out todo.luis
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\en\faq.lu -o %0\..\..\..\DeploymentScripts\en -n faq.qna
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\en\dispatch.lu -o %0\..\..\..\DeploymentScripts\en --out dispatch.luis -n Dispatch -i Dispatch

@echo on