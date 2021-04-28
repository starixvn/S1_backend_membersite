Name=registry.n88dev.com/membersite

Tag=$(git rev-parse --short HEAD)

Img=${Name}:${Tag}

Latest=${Name}:latest

sudo docker build -f ./scripts/dockerfile -t ${Img} .

sudo docker tag ${Img} ${Latest}

sudo docker push ${Name}