@echo off
color 0A
@echo 〓〓〓〓〓〓【git状态】〓〓〓〓〓〓
git status
@echo.
@echo.
@echo.
@echo -----------------------------------------------------------
@echo 〓〓〓〓〓〓【发布设置】〓〓〓〓〓〓
@echo 包路径 : %1                
@echo 分支 : %2   
@echo 版本号 : %3
@echo 启动latest：%4
@echo. 
rem @echo 请确保完成推送后再发布（按Enter执行发布）
:makesure
set var=""
set/p var=请确保完成推送后再发布（按Enter执行发布）
if %var%=="" (goto publish) else goto makesure
:publish
@echo 〓〓〓〓〓〓【发布中...】〓〓〓〓〓〓
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

@echo  按任意键退出...
pause>nul
exit