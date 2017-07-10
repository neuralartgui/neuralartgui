FROM ubuntu:14.04
MAINTAINER Maximilian Müller <github@wabnap.de> Nels Appold<>

# General dependencies, lots of them
RUN apt-get update && apt-get upgrade -y && apt-get install -y git
RUN apt-get install -y libprotobuf-dev libleveldb-dev libsnappy-dev libopencv-dev libhdf5-serial-dev libatlas-dev libzmq3-dev libboost-all-dev libgflags-dev libgoogle-glog-dev liblmdb-dev protobuf-compiler bc libopenblas-dev

RUN apt-get -y install \
	python \
	build-essential \
	python-dev \
	python-pip \
    python-numpy \
    python-scipy \
	wget \
	unzip \
	ipython \
	perl \
	libatlas-base-dev \
	gcc \
	gfortran \
	g++ \ 
	curl \
	lua5.2 \
	liblua5.2-dev \
	qt-sdk \ 
	software-properties-common

# Torch
WORKDIR /home
RUN curl -sk https://raw.githubusercontent.com/torch/ezinstall/master/install-deps | bash
RUN git clone https://github.com/torch/distro
RUN mv distro torch
WORKDIR /home/torch
RUN ./install.sh

# Luarocks
WORKDIR /home
RUN \
  wget http://luarocks.org/releases/luarocks-2.2.0.tar.gz && \
  tar -xzvf luarocks-2.2.0.tar.gz && \
  rm -f luarocks-2.2.0.tar.gz && \
  cd luarocks-2.2.0 && \
  ./configure && \
  make build && \
  make install && \
  make clean && \
  cd .. && \
  rm -rf luarocks-2.2.0
RUN /home/torch/install/bin/luarocks install nn

# Caffe
RUN git clone https://github.com/BVLC/caffe.git /caffe
WORKDIR /caffe
RUN cp Makefile.config.example Makefile.config
RUN easy_install --upgrade pip

# Enable CPU-only + openblas (faster than atlas)
RUN sed -i 's/# CPU_ONLY/CPU_ONLY/g' Makefile.config
RUN sed -i 's/BLAS := atlas/BLAS := open/g' Makefile.config

# Caffe's Python dependencies...
RUN pip install -r python/requirements.txt
RUN make all
RUN make pycaffe
ENV PYTHONPATH=/caffe/python

# Download model
RUN scripts/download_model_binary.py models/bvlc_googlenet

RUN sudo pip install numpy --upgrade

# Load Scripts
VOLUME ["/data"]

WORKDIR /
RUN mkdir /deepdream
ADD data/deepdream /deepdream


RUN mkdir /neuralart
ADD data/neuralart /neuralart

RUN echo "downloading inception weights"
RUN curl "http://kaishengtai.github.io/static/projects/neuralart/inception_caffe.th" -o /neuralart/models/inception_caffe.th
RUN echo "downloading vgg weights"
RUN curl "http://kaishengtai.github.io/static/projects/neuralart/vgg_normalized.th" -o /neuralart/models/vgg_normalized.th


RUN mkdir /data/output