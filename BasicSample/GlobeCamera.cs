using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX;

namespace BasicSample
{
    class GlobeCamera
    {
        #region メンバ

        /// <summary>
        /// 射影行列
        /// </summary>
        public Matrix ProjectionMat
        {
            get { return Matrix.PerspectiveFovLH(0.35f, 800f / 600, 0.01f, 100f); }
        }

        /// <summary>
        /// ビュー行列
        /// </summary>
        public Matrix ViewMat
        {
            get { return Matrix.LookAtLH(CameraPosition, LookAtPosition, new Vector3(0.0f, 1.0f, 0.0f)); }
        }

        /// <summary>
        /// カメラ位置
        /// </summary>
        public Vector3 CameraPosition;

        /// <summary>
        /// 視点
        /// </summary>
        public Vector3 LookAtPosition;

        public Vector3 CamRot;

        public float distants = 10.0f;

        public float radius = 8.0f;

        private int lastWhile = 0;

        private int lastX = 0, lastY = 0;

        int currentX, currentY;
        bool currentR;
        int deltaW;

        #endregion

        /// <summary>
        /// デフォルトのコンストラクタ
        /// </summary>
        public GlobeCamera(Form form)
        {
            form.MouseDown += (object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    currentR = true;
                }
            };
            form.MouseUp += (object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    currentR = false;
                }
            };
            form.MouseMove += (object sender, MouseEventArgs e) =>
            {
                currentX = e.X;
                currentY = e.Y;
            };
            form.MouseWheel += (object sender, MouseEventArgs e) =>
            {
                deltaW += e.Delta;
            };

        }

        /// <summary>
        /// デフォルトのデストラクタ
        /// </summary>
        ~GlobeCamera()
        {
        }

        public void OnUpdate(float elapsedTime)
        {
            // スクロールでカメラの距離
            radius -= (float)(deltaW) * 0.01f;
            radius = Math.Max(5f, radius);
            deltaW = 0;

            if (currentR)
            {
                CamRot.Y -= (currentX - lastX) * 0.01f;
                CamRot.X += (currentY - lastY) * 0.01f;
            }
            CamRot.X = Clamp(CamRot.X, -1.4f, 1.4f);
            lastX = currentX;
            lastY = currentY;
            CameraPosition = LookAtPosition + AxisToForce(CamRot.X, CamRot.Y) * radius;
        }

        /// <summary>
        /// <para> 方向ベクトルからXY軸角を生成 </para>
        /// <para> 標準化不要 </para>
        /// </summary>
        /// <param name="normal">方向ベクトル</param>
        /// <returns>ラジアン値のXY軸ベクトル</returns>
        private static Vector3 ForceToAxis(Vector3 vector)
        {
            float sqrt = (float)Math.Sqrt(vector.Z * vector.Z + vector.X * vector.X);

            float rotY = -(float)Math.Atan2(vector.Z, vector.X);
            float rotX = -(float)Math.Atan2(vector.Y, sqrt);

            return new Vector3(rotX, rotY, 0.0f);
        }

        /// <summary>
        /// <para> XY軸角から方向ベクトルを生成 </para>
        /// <para> X=90°:(0,1,0) </para>
        /// <para> Y=0° :(1,0,0) </para>
        /// </summary>
        private static Vector3 AxisToForce(float radX, float radY)
        {
            float cosY = (float)Math.Cos((double)radY);
            float cosX = (float)Math.Cos((double)radX);
            float sinY = (float)Math.Sin((double)radY);
            float sinX = (float)Math.Sin((double)radX);

            return new Vector3(cosY * cosX, sinX, sinY * cosX);
        }

        /// <summary>
        /// 値を指定された範囲に収める
        /// </summary>
        /// <param name="source">値</param>
        /// <param name="min">下限</param>
        /// <param name="max">上限</param>
        /// <returns>範囲に収められた値</returns>
        private static float Clamp(float source, float min, float max)
        {
            if (source < min)
            {
                source = min;
            }
            else if (source > max)
            {
                source = max;
            }
            return source;
        }

    }
}