@echo off

ECHO Generating zh-cn LUIS and QnA Maker models from .lu files ..
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\zh-cn\general.lu -o %0\..\..\..\DeploymentScripts\zh-cn -n General --out general.luis
call ludown parse toluis --in %0\..\..\..\CognitiveModels\LUIS\zh-cn\dispatch.lu -o %0\..\..\..\DeploymentScripts\zh-cn --out dispatch.luis -n Dispatch -i Dispatch
call ludown parse toqna  --in %0\..\..\..\CognitiveModels\QnA\zh-cn\faq.lu -o %0\..\..\..\DeploymentScripts\zh-cn -n faq.qna

@echo on