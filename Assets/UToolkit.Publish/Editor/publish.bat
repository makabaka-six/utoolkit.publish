@echo off
color 0A
@echo ��������������git״̬��������������
git status
@echo.
@echo.
@echo.
@echo -----------------------------------------------------------
@echo ���������������������á�������������
@echo ��·�� : %1                
@echo ��֧ : %2   
@echo �汾�� : %3
@echo ����latest��%4
@echo. 
rem @echo ��ȷ��������ͺ��ٷ�������Enterִ�з�����
:makesure
set var=""
set/p var=��ȷ��������ͺ��ٷ�������Enterִ�з�����
if %var%=="" (goto publish) else goto makesure
:publish
@echo ��������������������...��������������
git subtree split --prefix=%1 --branch %2
git tag %3  %2
git push origin %2 --tags

if %4=="false" (goto end)
git push origin --delete latest
git tag -d latest
git subtree split --prefix=%1 --branch %2
git tag latest  %2
git push origin %2 --tags

:end

@echo  ��������˳�...
pause>nul
exit