@echo off

ECHO Generating en-us LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\en\general.lu -o  %0\..\..\DeploymentScripts\en -n General --out general.luis
call ludown parse toluis --in  %0\..\..\..\skills\calendarskill\CognitiveModels\LUIS\en\calendar.lu -o  %0\..\..\DeploymentScripts\en -n Calendar --out calendar.luis
call ludown parse toluis --in  %0\..\..\..\skills\emailskill\CognitiveModels\LUIS\en\email.lu -o  %0\..\..\DeploymentScripts\en -n Email --out email.luis -n Email
call ludown parse toluis --in  %0\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\en\pointofinterest.lu -o  %0\..\..\DeploymentScripts\en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in  %0\..\..\..\skills\todoskill\CognitiveModels\LUIS\en\todo.lu -o  %0\..\..\DeploymentScripts\en -n ToDo --out todo.luis
call ludown parse toqna --in  %0\..\..\CognitiveModels\QnA\en\faq.lu -o  %0\..\..\DeploymentScripts\en -n faq.qna
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\en\dispatch.lu -o  %0\..\..\DeploymentScripts\en --out dispatch.luis -n Dispatch -i Dispatch

ECHO Generating de-de LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\de\general.lu -o  %0\..\..\DeploymentScripts\de --out general.luis -n General
call ludown parse toluis --in  %0\..\..\..\skills\calendarskill\CognitiveModels\LUIS\de\calendar.lu -o  %0\..\..\DeploymentScripts\de --out calendar.luis -n Calendar
call ludown parse toluis --in  %0\..\..\..\skills\emailskill\CognitiveModels\LUIS\de\email.lu -o  %0\..\..\DeploymentScripts\de --out email.luis -n Email
call ludown parse toluis --in  %0\..\..\..\skills\todoskill\CognitiveModels\LUIS\de\todo.lu -o  %0\..\..\DeploymentScripts\de --out todo.luis -n ToDo
call ludown parse toluis --in  %0\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\de\pointofinterest.lu -o  %0\..\..\DeploymentScripts\de --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in  %0\..\..\CognitiveModels\QnA\de\faq.lu -o  %0\..\..\DeploymentScripts\de -n faq.qna
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\de\dispatch.lu -o  %0\..\..\DeploymentScripts\de --out dispatch.luis -n Dispatch -i Dispatch -c de-de

ECHO Generating es-es LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\es\general.lu -o  %0\..\..\DeploymentScripts\es --out general.luis -n General
call ludown parse toluis --in  %0\..\..\..\skills\calendarskill\CognitiveModels\LUIS\es\calendar.lu -o  %0\..\..\DeploymentScripts\es --out calendar.luis -n Calendar
call ludown parse toluis --in  %0\..\..\..\skills\emailskill\CognitiveModels\LUIS\es\email.lu -o  %0\..\..\DeploymentScripts\es --out email.luis -n Email
call ludown parse toluis --in  %0\..\..\..\skills\todoskill\CognitiveModels\LUIS\es\todo.lu -o  %0\..\..\DeploymentScripts\es --out todo.luis -n ToDo
call ludown parse toluis --in  %0\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\es\pointofinterest.lu -o  %0\..\..\DeploymentScripts\es --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in  %0\..\..\CognitiveModels\QnA\es\faq.lu -o  %0\..\..\DeploymentScripts\es -n faq.qna
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\es\dispatch.lu -o  %0\..\..\DeploymentScripts\es --out dispatch.luis -n Dispatch -i Dispatch -c es-es

ECHO Generating fr-fr LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\fr\general.lu -o  %0\..\..\DeploymentScripts\fr --out general.luis -n General
call ludown parse toluis --in  %0\..\..\..\skills\calendarskill\CognitiveModels\LUIS\fr\calendar.lu -o  %0\..\..\DeploymentScripts\fr --out calendar.luis -n Calendar
call ludown parse toluis --in  %0\..\..\..\skills\emailskill\CognitiveModels\LUIS\fr\email.lu -o  %0\..\..\DeploymentScripts\fr --out email.luis -n Email
call ludown parse toluis --in  %0\..\..\..\skills\todoskill\CognitiveModels\LUIS\fr\todo.lu -o  %0\..\..\DeploymentScripts\fr --out todo.luis -n ToDo
call ludown parse toluis --in  %0\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\fr\pointofinterest.lu -o  %0\..\..\DeploymentScripts\fr --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in  %0\..\..\CognitiveModels\QnA\fr\faq.lu -o  %0\..\..\DeploymentScripts\fr -n faq.qna
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\fr\dispatch.lu -o  %0\..\..\DeploymentScripts\fr --out dispatch.luis -n Dispatch -i Dispatch -c fr-fr

ECHO Generating it-it LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\it\general.lu -o  %0\..\..\DeploymentScripts\it --out general.luis -n General
call ludown parse toluis --in  %0\..\..\..\skills\calendarskill\CognitiveModels\LUIS\it\calendar.lu -o  %0\..\..\DeploymentScripts\it --out calendar.luis -n Calendar
call ludown parse toluis --in  %0\..\..\..\skills\emailskill\CognitiveModels\LUIS\it\email.lu -o  %0\..\..\DeploymentScripts\it --out email.luis -n Email
call ludown parse toluis --in  %0\..\..\..\skills\todoskill\CognitiveModels\LUIS\it\todo.lu -o  %0\..\..\DeploymentScripts\it --out todo.luis -n ToDo
call ludown parse toluis --in  %0\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\it\pointofinterest.lu -o  %0\..\..\DeploymentScripts\it --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in  %0\..\..\CognitiveModels\QnA\it\faq.lu -o  %0\..\..\DeploymentScripts\it -n faq.qna
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\it\dispatch.lu -o  %0\..\..\DeploymentScripts\it --out dispatch.luis -n Dispatch -i Dispatch -c it-it

ECHO Generating zh-hans LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\zh-hans\general.lu -o  %0\..\..\DeploymentScripts\zh-hans --out general.luis -n General
call ludown parse toluis --in  %0\..\..\..\skills\calendarskill\CognitiveModels\LUIS\zh-hans\calendar.lu -o  %0\..\..\DeploymentScripts\zh-hans --out calendar.luis -n Calendar
call ludown parse toluis --in  %0\..\..\..\skills\emailskill\CognitiveModels\LUIS\zh-hans\email.lu -o  %0\..\..\DeploymentScripts\zh-hans --out email.luis -n Email
call ludown parse toluis --in  %0\..\..\..\skills\todoskill\CognitiveModels\LUIS\zh-hans\todo.lu -o  %0\..\..\DeploymentScripts\zh-hans --out todo.luis -n ToDo
call ludown parse toluis --in  %0\..\..\..\skills\pointofinterestskill\CognitiveModels\LUIS\zh-hans\pointofinterest.lu -o  %0\..\..\DeploymentScripts\zh-hans --out pointofinterest.luis -n PointOfInterest
call ludown parse toqna --in  %0\..\..\CognitiveModels\QnA\zh-hans\faq.lu -o  %0\..\..\DeploymentScripts\zh-hans -n faq.qna
call ludown parse toluis --in  %0\..\..\CognitiveModels\LUIS\zh-hans\dispatch.lu -o  %0\..\..\DeploymentScripts\zh-hans --out dispatch.luis -n Dispatch -i Dispatch -c zh-cn

@echo on