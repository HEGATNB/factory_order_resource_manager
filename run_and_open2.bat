@echo off
chcp 65001 >nul
title Система управления производством

REM Переход в папку скрипта
cd /d "%~dp0"

echo ============================================
echo  СИСТЕМА УПРАВЛЕНИЯ ПРОИЗВОДСТВОМ
echo  Запуск с автоматическим открытием браузера
echo ============================================
echo.

REM Проверка существования папки проекта
if not exist "ProductionManagementSystem" (
    echo [ОШИБКА] Папка ProductionManagementSystem не найдена!
    echo Текущая папка: %CD%
    echo.
    pause
    exit /b 1
)

REM Переход в папку проекта
cd ProductionManagementSystem
echo [INFO] Рабочая папка: %CD%
echo.

REM Проверка .NET SDK
echo [1/4] Проверка .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ОШИБКА] .NET SDK не найден!
    echo Установите .NET SDK с https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo        .NET SDK %DOTNET_VERSION% [OK]
echo.

REM Очистка временных файлов
echo [2/4] Очистка временных файлов...
if exist "bin" (
    rmdir /S /Q "bin" 2>nul
    echo        Папка bin очищена
)
if exist "obj" (
    rmdir /S /Q "obj" 2>nul
    echo        Папка obj очищена
)
echo.

REM Восстановление пакетов
echo [3/4] Восстановление пакетов NuGet...
dotnet restore -v q
if %errorlevel% neq 0 (
    echo [ОШИБКА] Не удалось восстановить пакеты!
    echo.
    echo Попытка детального восстановления:
    dotnet restore
    pause
    exit /b 1
)
echo        Пакеты восстановлены [OK]
echo.

REM Сборка проекта
echo [4/4] Сборка проекта...
dotnet build -v q
if %errorlevel% neq 0 (
    echo [ОШИБКА] Ошибка сборки проекта!
    echo.
    echo Подробности:
    dotnet build
    pause
    exit /b 1
)
echo        Сборка выполнена [OK]
echo.

REM Настройка URL
set APP_URL=http://localhost:5000
set APP_URL_HTTPS=https://localhost:5001

echo ============================================
echo  ЗАПУСК ПРИЛОЖЕНИЯ
echo ============================================
echo.
echo Приложение запускается...
echo HTTP:  %APP_URL%
echo HTTPS: %APP_URL_HTTPS%
echo.

REM Запуск приложения в отдельном окне
echo Запуск сервера в отдельном окне...
start "ProductionManagementSystem" dotnet run --urls "%APP_URL%;%APP_URL_HTTPS%" --no-launch-profile

REM Ожидание запуска сервера
echo Ожидание запуска сервера...
timeout /t 5 /nobreak >nul

REM Проверка, запустился ли сервер
echo Проверка доступности сервера...
curl -s -o nul %APP_URL% 2>nul
if %errorlevel% equ 0 (
    echo Сервер успешно запущен!
) else (
    echo [ПРЕДУПРЕЖДЕНИЕ] Сервер может еще запускаться...
    timeout /t 3 /nobreak >nul
)

REM Открытие браузера
echo Открытие браузера...
start "" "%APP_URL%"

echo.
echo ============================================
echo  ПРИЛОЖЕНИЕ ЗАПУЩЕНО
echo ============================================
echo.
echo  Адрес: %APP_URL%
echo.
echo  Для остановки сервера:
echo  1. Найдите окно "ProductionManagementSystem"
echo  2. Нажмите Ctrl+C
echo  3. Или закройте это окно
echo.
echo  СТАТУС: Работает
echo ============================================
echo.

REM Возврат в исходную папку
cd /d "%~dp0"

pause