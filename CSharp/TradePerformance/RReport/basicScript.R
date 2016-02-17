library(ggplot2)
d<-ttFeed.TickBestHistory("EURCHF", Sys.Date()-1, Sys.Date())
d[,spread:=ask-bid]
ggplot(d, aes(spread)) + geom_histogram() + geom_density()
qqnorm(d$spread)
########################################

library(AppliedPredictiveModeling)
data(concrete)
library(caret)
set.seed(1000)
inTrain = createDataPartition(mixtures$CompressiveStrength, p = 3/4)[[1]]
training = mixtures[ inTrain,]
testing = mixtures[-inTrain,]

training$F_Age<- cut2(training$Age, g = 3)
ggplot(training, aes(x=seq_along(CompressiveStrength), y=CompressiveStrength)) + geom_point(aes(colour=F_Age)) 
#####################
# 4
####################
library(caret)
library(AppliedPredictiveModeling)
set.seed(3433)
data(AlzheimerDisease)
adData = data.frame(diagnosis,predictors)
inTrain = createDataPartition(adData$diagnosis, p = 3/4)[[1]]
training = adData[ inTrain,]
testing = adData[-inTrain,]

d<-training[, grep( "^[Ii][Ll].*", names(training)) ]
pp<- preProcess(d, method=c("center", "scale", "pca"), thresh=0.9)
pp
####################################
# 5
#######
library(caret)
library(AppliedPredictiveModeling)
set.seed(3433)
data(AlzheimerDisease)
adData = data.frame(diagnosis,predictors)
inTrain = createDataPartition(adData$diagnosis, p = 3/4)[[1]]
training = adData[ inTrain,]
testing = adData[-inTrain,]

d<-as.data.table(training[, grep( "^[Ii][Ll].*", names(training)) ])
d[,diagnosis:=training$diagnosis]

testing1 <- as.data.table(testing[, grep( "^[Ii][Ll].*", names(testing)) ])
testing1[,diagnosis:=testing$diagnosis]

non_pca_model <- train(diagnosis ~ ., data = d, method="glm")
non_pca_result <- confusionMatrix(testing1[, diagnosis], predict(non_pca_model, testing1[, -13, with=FALSE]))
non_pca_result
