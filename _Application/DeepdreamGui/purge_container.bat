FOR /f "tokens=*" %%i IN ('docker ps -q ') DO docker kill %%i
FOR /f "tokens=*" %%i IN ('docker ps -q -a ') DO docker rm %%i
