<template>
  <div class="login-container" @mousemove="handleMouseMove" @mouseleave="handleMouseLeave">
    <div class="background-animation" ref="bgAnimation">
      <!-- 飘落图标 -->
      <div
        v-for="item in floatingItems"
        :key="item.id"
        class="floating-icon"
        :class="'icon-' + item.type"
        :style="getItemStyle(item)"
      >
        <svg v-if="item.type === 'book'" viewBox="0 0 64 64" :width="item.size" :height="item.size">
          <rect x="8" y="4" width="48" height="56" rx="4" fill="none" stroke="currentColor" stroke-width="3"/>
          <line x1="32" y1="4" x2="32" y2="60" stroke="currentColor" stroke-width="2" opacity="0.5"/>
          <line x1="16" y1="18" x2="28" y2="18" stroke="currentColor" stroke-width="2" opacity="0.4"/>
          <line x1="16" y1="26" x2="28" y2="26" stroke="currentColor" stroke-width="2" opacity="0.4"/>
          <line x1="36" y1="18" x2="48" y2="18" stroke="currentColor" stroke-width="2" opacity="0.4"/>
          <line x1="36" y1="26" x2="48" y2="26" stroke="currentColor" stroke-width="2" opacity="0.4"/>
        </svg>
        <svg v-else-if="item.type === 'headphone'" viewBox="0 0 64 64" :width="item.size" :height="item.size">
          <path d="M12 36 C12 20, 52 20, 52 36" fill="none" stroke="currentColor" stroke-width="3"/>
          <rect x="8" y="34" width="10" height="18" rx="4" fill="none" stroke="currentColor" stroke-width="3"/>
          <rect x="46" y="34" width="10" height="18" rx="4" fill="none" stroke="currentColor" stroke-width="3"/>
        </svg>
        <svg v-else-if="item.type === 'mug'" viewBox="0 0 64 64" :width="item.size" :height="item.size">
          <rect x="10" y="20" width="34" height="32" rx="4" fill="none" stroke="currentColor" stroke-width="3"/>
          <path d="M44 28 C56 28, 56 44, 44 44" fill="none" stroke="currentColor" stroke-width="3"/>
          <path d="M20 14 C22 8, 26 8, 28 14" fill="none" stroke="currentColor" stroke-width="2" opacity="0.5"/>
          <path d="M28 12 C30 6, 34 6, 36 12" fill="none" stroke="currentColor" stroke-width="2" opacity="0.5"/>
        </svg>
        <svg v-else-if="item.type === 'lamp'" viewBox="0 0 64 64" :width="item.size" :height="item.size">
          <path d="M20 8 L32 40 L44 8 Z" fill="none" stroke="currentColor" stroke-width="3"/>
          <line x1="32" y1="40" x2="32" y2="54" stroke="currentColor" stroke-width="3"/>
          <line x1="22" y1="54" x2="42" y2="54" stroke="currentColor" stroke-width="3"/>
        </svg>
        <svg v-else viewBox="0 0 64 64" :width="item.size" :height="item.size">
          <circle cx="32" cy="32" r="20" fill="none" stroke="currentColor" stroke-width="3"/>
          <circle cx="32" cy="32" r="8" fill="none" stroke="currentColor" stroke-width="2"/>
          <line x1="32" y1="4" x2="32" y2="12" stroke="currentColor" stroke-width="2"/>
          <line x1="32" y1="52" x2="32" y2="60" stroke="currentColor" stroke-width="2"/>
          <line x1="4" y1="32" x2="12" y2="32" stroke="currentColor" stroke-width="2"/>
          <line x1="52" y1="32" x2="60" y2="32" stroke="currentColor" stroke-width="2"/>
        </svg>
      </div>
      <!-- 装饰性光晕 -->
      <div class="glow glow-1" ref="glow1"></div>
      <div class="glow glow-2" ref="glow2"></div>
      <div class="glow glow-3" ref="glow3"></div>
    </div>
    <div class="login-content">
      <div class="login-left">
        <div class="welcome-text">
          <h1>校园二手交易平台</h1>
          <p>让闲置物品流转起来，创造更多价值</p>
        </div>
        <div class="feature-list">
          <div class="feature-item">
            <i class="el-icon-goods"></i>
            <span>丰富商品</span>
          </div>
          <div class="feature-item">
            <i class="el-icon-s-check"></i>
            <span>安全保障</span>
          </div>
          <div class="feature-item">
            <i class="el-icon-s-promotion"></i>
            <span>便捷交易</span>
          </div>
        </div>
      </div>
      <el-card class="login-card">
        <div class="login-body">
          <div class="login-header">
            <div class="logo-icon">
              <i class="el-icon-s-platform"></i>
            </div>
            <div class="login-title">后台管理系统</div>
            <div class="login-subtitle">管理员登录</div>
          </div>
          <el-form ref="form" :model="userForm">
            <div class="input-wrapper">
              <el-input placeholder="请输入管理员账号" v-model="userForm.accountNumber" class="login-input">
                <template v-slot:prepend>
                  <i class="el-icon-user-solid"></i>
                </template>
              </el-input>
            </div>
            <div class="input-wrapper">
              <el-input placeholder="请输入管理员密码" v-model="userForm.adminPassword" class="login-input"
                        @keyup.enter.native="login" show-password>
                <template v-slot:prepend>
                  <i class="el-icon-lock"></i>
                </template>
              </el-input>
            </div>
            <div class="login-submit">
              <el-button type="primary" @click="login" class="login-button">
                <span>登 录</span>
              </el-button>
            </div>
            <div class="other-submit">
              <router-link to="/login" class="sign-in-text">
                <i class="el-icon-user"></i>
                学生登录
              </router-link>
            </div>
          </el-form>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script>
export default {
  name: "login-admin",
  data() {
    return {
      userForm: {
        accountNumber: '',
        adminPassword: ''
      },
      mouseX: 0,
      mouseY: 0,
      currentOffsetX: 0,
      currentOffsetY: 0,
      animationFrame: null,
      floatingItems: []
    };
  },
  created() {
    this.initFloatingItems();
  },
  mounted() {
    this.startParallaxLoop();
  },
  beforeDestroy() {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
    }
  },
  methods: {
    // 初始化飘落图标
    initFloatingItems() {
      const types = ['book', 'headphone', 'mug', 'lamp', 'disc'];
      const items = [];
      for (let i = 0; i < 12; i++) {
        items.push(this.createFloatingItem(i, types));
      }
      this.floatingItems = items;
    },
    createFloatingItem(index, types) {
      return {
        id: index,
        type: types[index % types.length],
        size: 28 + Math.random() * 24,
        left: Math.random() * 100,
        top: Math.random() * 100,
        duration: 18 + Math.random() * 20,
        delay: -(Math.random() * 20),
        driftDuration: 12 + Math.random() * 10,
        driftDelay: -(Math.random() * 10),
        rotateDuration: 25 + Math.random() * 15,
        parallaxDepth: 0.3 + Math.random() * 0.7
      };
    },
    getItemStyle(item) {
      return {
        left: item.left + '%',
        top: item.top + '%',
        animation: `iconFloat ${item.duration}s ${item.delay}s infinite ease-in-out, iconDrift ${item.driftDuration}s ${item.driftDelay}s infinite ease-in-out, iconRotate ${item.rotateDuration}s infinite linear`
      };
    },
    // 鼠标视差效果
    handleMouseMove(e) {
      const rect = this.$el.getBoundingClientRect();
      this.mouseX = (e.clientX - rect.left - rect.width / 2) / rect.width;
      this.mouseY = (e.clientY - rect.top - rect.height / 2) / rect.height;
    },
    handleMouseLeave() {
      this.mouseX = 0;
      this.mouseY = 0;
    },
    startParallaxLoop() {
      const smoothFactor = 0.03;
      const loop = () => {
        this.currentOffsetX += (this.mouseX * 30 - this.currentOffsetX) * smoothFactor;
        this.currentOffsetY += (this.mouseY * 30 - this.currentOffsetY) * smoothFactor;
        // 更新光晕位置
        if (this.$refs.glow1) {
          this.$refs.glow1.style.transform = `translate(${this.currentOffsetX * 1.2}px, ${this.currentOffsetY * 1.2}px)`;
        }
        if (this.$refs.glow2) {
          this.$refs.glow2.style.transform = `translate(${this.currentOffsetX * 0.8}px, ${this.currentOffsetY * 0.8}px)`;
        }
        if (this.$refs.glow3) {
          this.$refs.glow3.style.transform = `translate(${this.currentOffsetX * 1.5}px, ${this.currentOffsetY * 1.5}px)`;
        }
        // 更新飘落图标位置
        this.floatingItems.forEach((item, i) => {
          const el = this.$el.querySelector(`.floating-icon:nth-child(${i + 1})`);
          if (el) {
            const depth = item.parallaxDepth;
            el.style.transform = `translate(${this.currentOffsetX * depth}px, ${this.currentOffsetY * depth}px)`;
          }
        });
        this.animationFrame = requestAnimationFrame(loop);
      };
      loop();
    },
    login() {
      // 测试模式：允许空账号密码直接登录
      if (!this.userForm.accountNumber && !this.userForm.adminPassword) {
        this.$message.warning('测试模式：使用默认管理员账号登录');
        localStorage.setItem('adminToken', 'test-token');
        localStorage.setItem('adminInfo', JSON.stringify({username: '测试管理员'}));
        this.$sta.isLogin = true;
        this.$sta.adminName = '测试管理员';
        this.$router.replace({path: '/platform-admin'});
        return;
      }

      // 适配新 API 参数格式
      this.$api.adminLogin({
        username: this.userForm.accountNumber,   // 字段名改为 username
        password: this.userForm.adminPassword    // 字段名改为 password
      }).then(res => {
        console.log(res);
        if (res.status_code === 1) {
          // 保存 token 和管理员信息
          if (res.data && res.data.token) {
            localStorage.setItem('adminToken', res.data.token);
            localStorage.setItem('adminInfo', JSON.stringify(res.data.userInfo));
          }
          this.$sta.isLogin = true;
          this.$sta.adminName = res.data.userInfo?.username || '管理员';
          this.$router.replace({path: '/platform-admin'});
        } else {
          this.$message.error(res.msg || '登录失败，账号或密码错误！');
        }
      }).catch(e => {
        console.log(e);
        this.$message.error('登录失败，请检查网络！');
      });
    }
  }
}
</script>

<style scoped>
.login-container {
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  width: 100%;
  overflow: hidden;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

/* 动态背景动画 */
.background-animation {
  position: absolute;
  width: 100%;
  height: 100%;
  overflow: hidden;
  z-index: 0;
  pointer-events: none;
}

/* 飘落图标 */
.floating-icon {
  position: absolute;
  color: rgba(255, 255, 255, 0.15);
  will-change: transform;
  filter: drop-shadow(0 0 6px rgba(255, 255, 255, 0.08));
  transition: color 0.3s ease;
}

.floating-icon:hover {
  color: rgba(255, 255, 255, 0.3);
}

@keyframes iconFloat {
  0%, 100% {
    margin-top: 0;
  }
  50% {
    margin-top: -25px;
  }
}

@keyframes iconDrift {
  0%, 100% {
    margin-left: 0;
  }
  25% {
    margin-left: 15px;
  }
  75% {
    margin-left: -15px;
  }
}

@keyframes iconRotate {
  0% {
    rotate: 0deg;
  }
  100% {
    rotate: 360deg;
  }
}

/* 装饰性光晕 */
.glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.12;
  transition: transform 0.1s ease-out;
  will-change: transform;
}

.glow-1 {
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(255,255,255,0.4), transparent 70%);
  top: -5%;
  left: -5%;
}

.glow-2 {
  width: 350px;
  height: 350px;
  background: radial-gradient(circle, rgba(200,180,255,0.3), transparent 70%);
  bottom: -10%;
  right: -5%;
}

.glow-3 {
  width: 300px;
  height: 300px;
  background: radial-gradient(circle, rgba(255,200,220,0.3), transparent 70%);
  top: 40%;
  left: 50%;
}

/* 登录内容区域 */
.login-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 60px;
  z-index: 1;
  width: 100%;
  max-width: 1200px;
  padding: 0 20px;
}

/* 左侧欢迎区域 */
.login-left {
  color: #fff;
  flex: 1;
  max-width: 500px;
}

.welcome-text {
  margin-bottom: 50px;
}

.welcome-text h1 {
  font-size: 48px;
  font-weight: 700;
  margin-bottom: 20px;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.1);
}

.welcome-text p {
  font-size: 20px;
  opacity: 0.9;
}

.feature-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 15px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 10px;
  backdrop-filter: blur(10px);
  transition: all 0.3s ease;
}

.feature-item:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: translateX(10px);
}

.feature-item i {
  font-size: 28px;
}

.feature-item span {
  font-size: 18px;
  font-weight: 500;
}

/* 登录卡片 */
.login-card {
  width: 420px;
  border-radius: 20px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(10px);
  background: rgba(255, 255, 255, 0.95);
  animation: slideIn 0.5s ease-out;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(50px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.login-body {
  padding: 40px 30px;
}

.login-header {
  text-align: center;
  margin-bottom: 35px;
}

.logo-icon {
  width: 70px;
  height: 70px;
  margin: 0 auto 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 36px;
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
}

.login-title {
  font-size: 26px;
  font-weight: 700;
  color: #333;
  margin-bottom: 8px;
}

.login-subtitle {
  font-size: 14px;
  color: #999;
}

.input-wrapper {
  margin-bottom: 20px;
  position: relative;
}

.login-input {
  transition: all 0.3s ease;
}

.login-input /deep/ .el-input__inner {
  height: 48px;
  border-radius: 8px;
  border: 2px solid #e0e0e0;
  padding-left: 45px;
  transition: all 0.3s ease;
}

.login-input >>> .el-input__inner:focus {
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.login-input /deep/ .el-input-group__prepend {
  background: transparent;
  border: none;
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  padding: 0;
  z-index: 10;
}

.login-input >>> .el-input-group__prepend i {
  font-size: 20px;
  color: #999;
  transition: color 0.3s ease;
}

.login-input >>> .el-input__inner:focus + .el-input-group__prepend i,
.login-input >>> .el-input__inner:focus ~ .el-input-group__prepend i {
  color: #667eea;
}

.login-submit {
  margin-top: 30px;
}

.login-button {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 600;
  border-radius: 8px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
  transition: all 0.3s ease;
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
}

.login-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 25px rgba(102, 126, 234, 0.5);
}

.login-button:active {
  transform: translateY(0);
}

.other-submit {
  display: flex;
  justify-content: center;
  margin-top: 20px;
}

.sign-in-text {
  color: #667eea;
  font-size: 15px;
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 5px;
  transition: all 0.3s ease;
  padding: 8px 16px;
  border-radius: 20px;
}

.sign-in-text:hover {
  background: rgba(102, 126, 234, 0.1);
  transform: scale(1.05);
}

/* 响应式设计 */
@media (max-width: 960px) {
  .login-left {
    display: none;
  }

  .login-card {
    width: 100%;
    max-width: 420px;
  }
}
</style>