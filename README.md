# DeepdreamGui

WPF-based user interface for Google's [Deepdream](https://github.com/google/deepdream/) and Kaishengtai's [Implementation](https://github.com/kaishengtai/neuralart) of [A Neural Algorithm of Artistic Style](https://arxiv.org/abs/1508.06576). 
Uses merged and modified Docker Container of [Herval](https://github.com/herval/deepdream-docker) and [mbartoli](https://github.com/mbartoli/docker-neuralart).

Made by [@muelmx](https://github.com/muelmx) & [@Loizz](https://github.com/Loizz)

> *Note:* This is made for creative people. For the full set of options and maximal performance use the command line tools and a gpu accelerated installation.

![Screenshot](https://raw.githubusercontent.com/neuralartgui/neuralartgui-website/master/img/screenshot01-cmp.jpg "Screenshot")
![Screenshot](https://raw.githubusercontent.com/neuralartgui/neuralartgui-website/master/img/screenshot02-cmp.jpg "Screenshot")


## Prequisites
1. Windows PC with .Net Framework Version >= 4.5 and a Virtualization enabled CPU
2. Working *Docker* Installation. You can get *Docker* at https://www.docker.com/products/docker
   
   > Different versions of Windows might need different versions of Docker. Read the Instructions carefully.

## Installation
### 1. Install Docker

- Option 1 (Recommended): Install Docker in Hyper-V
*You need a Hyper-V enabled Version of Windows 8/10*

1. Download [https://www.docker.com/products/docker#/windows](https://www.docker.com/products/docker#/windows)
2. Install
3. Increase RAM and CPU in Docker Settings

- Option 2: Install Docker Toolbox based on Virtual Box

1. Download [https://www.docker.com/products/docker-toolbox](https://www.docker.com/products/docker-toolbox)
2. Install to default location
3. Run *Docker Quickstart Terminal* to create *default* Virtual Machine
4. Increase RAM and CPU of default VM in Virtual Box Manager.



### 2. Download
- Download the current release or clone the repository for bleeding edge features

### 3. Build Docker Images
1. Start Docker
2. Depending on your System Run `_CombinedContainer\install_toolbox.bat` or `_CombinedContainer\install_hyperv.bat`
*This depends on your connection speed and easily takes half an hour, you can ignore the red output text as long as the script finishes with "done"*

### 4. Build/Run GUI
Get current release from [releases](https://github.com/neuralartgui/neuralartgui/releases)

or

Build Project using Visual Studio 2015 or newer.
Run Application either from Visual Studio or *_Application/DeepdreamGui/bin/Debug/DeepdreamGui.exe* directly.

## How To Use
- Deepdream
1. Select image
2. (Optional) Select trigger pattern to trigger Model
3. (Optional) Move trigger pattern
4. Select Model
5. Adjust parameters
6. Click 'Run'
7. Get a coffee (computing images will take a while)
8. Enjoy

- Style
1. Select base image
2. Select style image
3. Adjust parameters
4. (Optional) Enable shrinking if you don't have a lot of memory
5. Get a coffee (computing images will take a while)
8. Enjoy

- Reports

    You can save a set of result images to a html report, containing full resolution images and additional information, like comments, used parameters and the name of the model. 
1. Select result images
2. Click 'generate report'
3. Enter data
4. Select save location 

## Licenses
+ [MaterialDesignIcons](https://materialdesignicons.com/) by Google and Contributors. License: [OFL-1.1](https://github.com/Templarian/MaterialDesign/blob/master/license.txt)
+ [MahAppsMetro](https://github.com/MahApps/MahApps.Metro) License: [MIT](https://github.com/MahApps/MahApps.Metro/blob/develop/LICENSE)


## HelpFiles
You can generate the source code documentation yourself by running [Doxygen](http://www.stack.nl/~dimitri/doxygen/index.html) with the config provided in */_HelpFiles*
