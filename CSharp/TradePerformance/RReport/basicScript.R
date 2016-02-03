require(data.table)

d<-read.csv("https://raw.githubusercontent.com/SoftFx/FdkUtilities/master/CSharp/TradePerformance/Results/TradePerformance_Development_1.txt", header=T, sep = ",")
d<-as.data.table(d)

si<-rbind( tapply(d$Total, d$OrdPerSec, length), 
           tapply(d$Total, d$OrdPerSec, min),
           tapply(d$Total, d$OrdPerSec, mean), 
           tapply(d$Total, d$OrdPerSec, median), 
           tapply(d$Total, d$OrdPerSec, max), 
           tapply(d$Total, d$OrdPerSec, sd))
rownames(si)<-c("length", "min", "mean", "median", "max", "sd")
si<-round(t(si))


ggplot( d, aes(x=OrdPerSec, y=OrdPerSec_Mean)) + geom_line() + geom_smooth(method = "lm", se = FALSE)#geom_abline(intercept = 0, slope = 1, col="Red") )
