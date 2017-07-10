docker-machine start default

docker-machine env --shell cmd default
FOR /f "tokens=*" %%i IN ('docker-machine env --shell cmd default') DO %%i

docker rmi -f muelmx/neuralart_exec
docker build -t muelmx/neuralart_exec .

echo done
pause