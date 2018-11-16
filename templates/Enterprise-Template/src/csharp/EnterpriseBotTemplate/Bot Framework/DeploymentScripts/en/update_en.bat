@echo off

ECHO Generating en-us LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\en\general.lu -o %0\..\..\..\DeploymentScripts\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\en\dispatch.lu -o %0\..\..\..\DeploymentScripts\en --out dispatch.luis -n Dispatch -i Dispatch
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\en\faq.lu -o %0\..\..\..\DeploymentScripts\en -n faq.qna

@echo on