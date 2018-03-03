using System;
using System.Collections.Generic;
using System.Globalization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace DelsysPlugin
{
    public class Classifier
    {
        public int FeaWinWidth { get; private set; }
        public int StepLength { get; private set; }
        public int FeaDimension { get; private set; }
        public int ChannelNum { get; private set; }
        public int GestureNumber { get; private set; }

        public List<List<double>> ModelMean { get; } = new List<List<double>>();
        public List<List<double>> ModelCov { get; } = new List<List<double>>();

        private readonly List<List<double>> _mFeatureMatrix = new List<List<double>>();
        private readonly List<int> _mLabelVector = new List<int>();
        private readonly List<int> _mClassLabel = new List<int>();

        public Classifier(int feaWinWidth, int stepLength,int channelNum, int gestureNumber)
        {
            FeaWinWidth = feaWinWidth;
            StepLength = stepLength;
            FeaDimension = channelNum * 4;
            ChannelNum = channelNum;
            GestureNumber = gestureNumber;
        }

        public double RestState(List<List<double>> dataWindow)
        {
            var emgEnergy = new double[ChannelNum];
            double energy = 0;
            for (var j = 0; j < ChannelNum; j++)
                foreach (var t in dataWindow)
                    emgEnergy[j] = emgEnergy[j] + t[j] * t[j];
            for (var i = 0; i < ChannelNum; i++)
                energy = energy + emgEnergy[i];
            energy = energy / dataWindow.Count / dataWindow[0].Count;
            return energy;
        }

        public int AddFeatureLabelFromData(List<List<double>> dataMatrix, int[] label)
        {
            for (var smpIdx = 0; smpIdx + FeaWinWidth < dataMatrix.Count; smpIdx += StepLength)
            {
                var dataWin = new List<List<double>>();
                for (var i = 0; i < FeaWinWidth; i++)
                {
                    var temp = new List<double>();
                    for (var j = 0; j < ChannelNum; j++)
                        temp.Add(dataMatrix[smpIdx + i][j]);
                    dataWin.Add(temp);
                }
                var feaTemp = FeatureExtractToVec(dataWin);
                var labelTemp = label[smpIdx + FeaWinWidth / 2];
                _mFeatureMatrix.Add(feaTemp);
                _mLabelVector.Add(labelTemp);
            }
            return _mFeatureMatrix.Count;
        }

        private List<double> FeatureExtractToVec(List<List<double>> dataMatrix)
        {
            var feaTemp = new List<double>();
            for (var i = 0; i < FeaDimension; i++)
                feaTemp.Add(0.0);
            for (var smpIdx = 0; smpIdx < dataMatrix.Count; smpIdx += FeaWinWidth)
            for (var i = 0; i < ChannelNum; i++)
            {
                var channelIndex = i;
                feaTemp[i * 4] = 0;
                feaTemp[i * 4 + 1] = 0;
                feaTemp[i * 4 + 2] = 0;
                feaTemp[i * 4 + 3] = 0;
                for (var j = 0; j < FeaWinWidth; j++)
                {
                    var j0 = smpIdx + j;
                    var j1 = j0 - 1;
                    var j2 = j0 - 2;
                    feaTemp[i * 4] += Math.Abs(dataMatrix[j0][channelIndex]);
                    if (j > 0)
                    {
                        feaTemp[i * 4 + 1] += dataMatrix[j0][channelIndex] * dataMatrix[j1][channelIndex] > 0 ? 1 : 0;
                        feaTemp[i * 4 + 2] += Math.Abs(dataMatrix[j0][channelIndex] - dataMatrix[j1][channelIndex]);
                    }
                    if (j > 1)
                        feaTemp[i * 4 + 3] += (dataMatrix[j0][channelIndex] - dataMatrix[j1][channelIndex]) *
                                              (dataMatrix[j1][channelIndex] - dataMatrix[j2][channelIndex]) > 0
                            ? 1
                            : 0;
                }
            }
            return feaTemp;
        }

        public bool GenerateModel()
        {
            bool success = BayesTrain(_mFeatureMatrix, _mLabelVector);
            return success;
        }

        private bool BayesTrain(List<List<double>> feature, List<int> label)
        {
            var featNum = feature.Count;
            var featDim = feature[0].Count;
            var labNum = label.Count;
            if (labNum != featNum) return false;

            for (var i = 0; i < GestureNumber; i++) _mClassLabel.Add(i);

            Matrix<double> featMat = new DenseMatrix(featNum, featDim);
            for (var i = 0; i < featNum; i++)
            for (var j = 0; j < featDim; j++)
                featMat[i, j] = feature[i][j];

            var cNum = _mClassLabel.Count;
            Matrix<double> meanMat = new DenseMatrix(cNum, featDim);
            Matrix<double> covMat = new DenseMatrix(featDim * cNum, featDim);
            Matrix<double> poolCovMat = new DenseMatrix(featDim, featDim);
            Vector<double> numPerClass = new DenseVector(cNum);
            // compute the mean vector for each class
            for (var i = 0; i < featNum; i++)
            for (var j = 0; j < cNum; j++)
            {
                if (label[i] != _mClassLabel[j]) continue;
                meanMat.SetRow(j, meanMat.Row(j) + featMat.Row(i));
                numPerClass.At(j, numPerClass.At(j) + 1);
            }
            for (var i = 0; i < cNum; i++)
                meanMat.SetRow(i, meanMat.Row(i) / numPerClass.At(i));
            //compute the covariance matrix for each class and pool covariance matrix
            for (var i = 0; i < featNum; i++)
            for (var j = 0; j < cNum; j++)
                if (label[i] == _mClassLabel[j])
                    covMat.SetSubMatrix(j * featDim, featDim, 0, featDim,
                        covMat.SubMatrix(j * featDim, featDim, 0, featDim) +
                        (featMat.Row(i) - meanMat.Row(j)).OuterProduct(featMat.Row(i) - meanMat.Row(j)));
            for (var i = 0; i < cNum; i++)
            {
                poolCovMat += covMat.SubMatrix(i * featDim, featDim, 0, featDim);
                //PoolCovMat += CovMat.block(i* feat_dim,0,feat_dim,feat_dim);
                covMat.SetSubMatrix(i * featDim, featDim, 0, featDim,
                    covMat.SubMatrix(i * featDim, featDim, 0, featDim) / (numPerClass.At(i) - 1));
                //CovMat.block(i* feat_dim,0,feat_dim,feat_dim)=CovMat.block(i* feat_dim,0,feat_dim,feat_dim)/(feat_num_perclass(i)-1);
            }
            poolCovMat /= featNum - cNum;
            poolCovMat = poolCovMat.Inverse();

            //transform the data format from Eigen to member vectors
            ModelMean?.Clear();
            ModelCov?.Clear();

            List<double> temp;
            for (var i = 0; i < cNum; i++)
            {
                temp = new List<double>();
                for (var j = 0; j < featDim; j++)
                    temp.Add(meanMat[i, j]);
                ModelMean?.Add(temp);
            }
            for (var i = 0; i < featDim; i++)
            {
                temp = new List<double>();
                for (var j = 0; j < featDim; j++)
                    temp.Add(poolCovMat[i, j]);
                ModelCov?.Add(temp);
            }
            return true;
        }

        public int Predict(List<List<double>> dataWindow)
        {
            var fea = FeatureExtractToVec(dataWindow);
            return BayesPredict(ModelCov, ModelMean, fea);
        }

        private int BayesPredict(List<List<double>> poolCovMat, List<List<double>> meanMat, List<double> x)
        {
            var featDim = poolCovMat.Count;
            var cNum = meanMat.Count;
            var dimX = x.Count;
            if (dimX != featDim)
            {
                return -1;
            }
            Matrix<double> feature = new DenseMatrix(1, featDim);
            Matrix<double> cov = new DenseMatrix(featDim, featDim);
            Matrix<double> mean = new DenseMatrix(cNum, featDim);
            int i, j;
            for (i = 0; i < featDim; i++)
                feature[0, i] = x[i];
            for (i = 0; i < featDim; i++)
            {
                for (j = 0; j < featDim; j++)
                {
                    cov[i, j] = poolCovMat[i][j];
                }
            }

            for (i = 0; i < cNum; i++)
            {
                for (j = 0; j < featDim; j++)
                {
                    mean[i, j] = meanMat[i][j];
                }        
            }

            double gx;
            double mingx = 0;
            var predict = 0;
            for (i = 0; i < cNum; i++)
            {
                gx = (feature.Row(0) - mean.Row(i)) * cov * (feature.Row(0) - mean.Row(i));
                if (i == 0)
                {
                    mingx = gx;
                    predict = _mClassLabel[i];
                }
                else
                {
                    if (!(gx < mingx)) continue;
                    mingx = gx;
                    predict = _mClassLabel[i];
                }
            }
            return predict;
        }
    }
}
