@echo off

ECHO Generating es LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\es\general.lu -o %0\..\..\..\DeploymentScripts\es -n General --out general.luis
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\es\dispatch.lu -o %0\..\..\..\DeploymentScripts\es --out dispatch.luis -n Dispatch -i Dispatch
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\es\faq.lu -o %0\..\..\..\DeploymentScripts\es -n faq.qna

@echo on