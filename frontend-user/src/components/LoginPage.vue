<template>
  <div class="login-container" @mousemove="handleMouseMove" @mouseleave="handleMouseLeave">
    <!-- 飘落图标动画 -->
    <div v-for="item in floatingItems" :key="item.id" class="floating-icon" :style="item.style">
      {{ item.icon }}
    </div>

    <!-- 光晕效果 -->
    <div ref="glow1" class="glow glow-1"></div>
    <div ref="glow2" class="glow glow-2"></div>
    <div ref="glow3" class="glow glow-3"></div>

    <div class="login-content">
      <!-- 左侧欢迎区域 -->
      <div class="login-left">
        <div class="welcome-title">
          <h1>欢迎回来</h1>
          <p>校园二手交易平台</p>
        </div>
        <div class="feature-list">
          <div class="feature-item">
            <span class="feature-icon">🛒</span>
            <div>
              <h3>安全交易</h3>
              <p>平台担保，交易无忧</p>
            </div>
          </div>
          <div class="feature-item">
            <span class="feature-icon">🔒</span>
            <div>
              <h3>隐私保护</h3>
              <p>信息安全，放心使用</p>
            </div>
          </div>
          <div class="feature-item">
            <span class="feature-icon">⚡</span>
            <div>
              <h3>快速发布</h3>
              <p>一键发布，轻松出售</p>
            </div>
          </div>
          <div class="feature-item">
            <span class="feature-icon">💰</span>
            <div>
              <h3>免费交易</h3>
              <p>零手续费，买卖更自由</p>
            </div>
          </div>
        </div>
      </div>

      <!-- 右侧登录卡片 -->
      <div class="login-card">
        <div class="card-header">
          <h2>用户登录</h2>
        </div>

        <div class="card-body">
          <div class="input-group">
            <label>用户名</label>
            <input 
              v-model="username" 
              type="text" 
              placeholder="请输入用户名（至少3个字符）"
              @input="clearError"
              @keyup.enter="handleLogin"
            />
          </div>
          <div class="input-group">
            <label>密码</label>
            <div class="password-wrapper">
              <input 
                v-model="password" 
                :type="showPassword ? 'text' : 'password'" 
                placeholder="请输入密码（至少6个字符）"
                @keyup.enter="handleLogin"
              />
              <span class="password-toggle" @click="showPassword = !showPassword">
                {{ showPassword ? '🙈' : '👁️' }}
              </span>
            </div>
          </div>
          <div v-if="errorMessage" class="error-message">
            {{ errorMessage }}
          </div>
          <button 
            class="login-button" 
            @click="handleLogin" 
            :disabled="loading"
          >
            {{ loading ? '登录中...' : '登录' }}
          </button>
          <div class="register-tip">
            还没有账号？<span @click="goToRegister">立即注册</span>
          </div>
          <div class="admin-link" @click="goToAdminLogin">
            🔐 管理员登录
          </div>
        </div>
      </div>
    </div>

    <!-- Toast 通知 -->
    <Transition name="toast">
      <div v-if="toastVisible" class="global-toast" :class="toastType">
        <span class="toast-icon">{{ toastIcon }}</span>
        <span class="toast-message">{{ toastMessage }}</span>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// ==================== 响应式数据 ====================
const username = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const errorMessage = ref('')

// API 基础地址
const API_BASE_URL = 'http://172.19.229.149:5000'

// Toast 相关
const toastVisible = ref(false)
const toastMessage = ref('')
const toastType = ref('success')
let toastTimer = null

// ==================== 动画相关 ====================
const mouseX = ref(0)
const mouseY = ref(0)
const currentOffsetX = ref(0)
const currentOffsetY = ref(0)
let animationFrame = null

const icons = ['🌟', '✨', '⭐', '💫', '⚡', '🌸', '🍃', '🪄', '🎈', '💎', '🦋', '🍀']

const floatingItems = ref([])

// 创建飘落图标
const createFloatingItems = () => {
  const items = []
  for (let i = 0; i < 30; i++) {
    items.push({
      id: i,
      icon: icons[i % icons.length],
      size: 24 + Math.random() * 28,
      left: Math.random() * 100,
      top: Math.random() * 100,
      duration: 15 + Math.random() * 25,
      delay: -(Math.random() * 20),
      driftDuration: 10 + Math.random() * 15,
      rotateDuration: 20 + Math.random() * 20,
      parallaxDepth: 0.2 + Math.random() * 0.8,
      style: {}
    })
  }
  
  items.forEach(item => {
    item.style = {
      fontSize: `${item.size}px`,
      left: `${item.left}%`,
      top: `${item.top}%`,
      animation: `iconFloat ${item.duration}s infinite ease-in-out,
                   iconDrift ${item.driftDuration}s infinite ease-in-out,
                   iconRotate ${item.rotateDuration}s infinite linear`,
      animationDelay: `${item.delay}s, ${item.delay * 0.8}s, ${item.delay * 0.6}s`
    }
  })
  
  return items
}

// 鼠标移动处理
const handleMouseMove = (e) => {
  const rect = e.currentTarget.getBoundingClientRect()
  mouseX.value = (e.clientX - rect.left) / rect.width - 0.5
  mouseY.value = (e.clientY - rect.top) / rect.height - 0.5
}

const handleMouseLeave = () => {
  mouseX.value = 0
  mouseY.value = 0
}

// 启动视差循环
const startParallaxLoop = () => {
  const smoothFactor = 0.03
  
  const loop = () => {
    currentOffsetX.value += (mouseX.value * 40 - currentOffsetX.value) * smoothFactor
    currentOffsetY.value += (mouseY.value * 40 - currentOffsetY.value) * smoothFactor
    
    const glow1 = document.querySelector('.glow-1')
    const glow2 = document.querySelector('.glow-2')
    const glow3 = document.querySelector('.glow-3')
    
    if (glow1) {
      glow1.style.transform = `translate(${currentOffsetX.value * 1.2}px, ${currentOffsetY.value * 1.2}px)`
    }
    if (glow2) {
      glow2.style.transform = `translate(${currentOffsetX.value * 0.8}px, ${currentOffsetY.value * 0.8}px)`
    }
    if (glow3) {
      glow3.style.transform = `translate(${currentOffsetX.value * 1.5}px, ${currentOffsetY.value * 1.5}px)`
    }
    
    const iconEls = document.querySelectorAll('.floating-icon')
    iconEls.forEach((el, i) => {
      if (el && floatingItems.value[i]) {
        el.style.transform = `translate(${currentOffsetX.value * floatingItems.value[i].parallaxDepth}px, ${currentOffsetY.value * floatingItems.value[i].parallaxDepth}px)`
      }
    })
    
    animationFrame = requestAnimationFrame(loop)
  }
  loop()
}

// ==================== Toast 通知 ====================
const toastIcon = computed(() => {
  const iconsMap = { success: '✓', error: '✗', warning: '⚠', info: 'ℹ' }
  return iconsMap[toastType.value] || 'ℹ'
})

const showToast = (message, type = 'success') => {
  if (toastTimer) clearTimeout(toastTimer)
  toastMessage.value = message
  toastType.value = type
  toastVisible.value = true
  toastTimer = setTimeout(() => {
    toastVisible.value = false
  }, 3000)
}

// ==================== 表单验证 ====================
const clearError = () => {
  errorMessage.value = ''
}

const validateForm = () => {
  if (!username.value.trim()) {
    errorMessage.value = '请输入用户名'
    return false
  }
  if (username.value.trim().length < 3) {
    errorMessage.value = '用户名长度不能少于3个字符'
    return false
  }
  if (!password.value) {
    errorMessage.value = '请输入密码'
    return false
  }
  if (password.value.length < 6) {
    errorMessage.value = '密码长度不能少于6个字符'
    return false
  }
  return true
}

// ==================== 用户登录 ====================
const handleLogin = async () => {
  if (!validateForm()) return
  
  loading.value = true
  errorMessage.value = ''
  
  try {
    const response = await fetch(`${API_BASE_URL}/api/Auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        username: username.value.trim(),
        password: password.value
      })
    })
    
    const data = await response.json()
    console.log('用户登录响应:', data)
    
    if (response.ok && (data.code === 200 || data.success)) {
      const userData = data.data || data.user
      if (data.data?.token) {
        localStorage.setItem('token', data.data.token)
      }
      localStorage.setItem('user', JSON.stringify({ 
        id: userData?.id || userData?.userId,
        username: username.value.trim(),
        phone: userData?.phone || '',
        defaultLocation: userData?.defaultLocation || ''
      }))
      showToast('登录成功！', 'success')
      setTimeout(() => {
        router.push('/')
      }, 1000)
    } else {
      errorMessage.value = data.message || data.msg || '用户名或密码错误'
      showToast(errorMessage.value, 'error')
    }
  } catch (err) {
    console.error('登录失败:', err)
    errorMessage.value = '网络异常，请检查网络连接'
    showToast(errorMessage.value, 'error')
  } finally {
    loading.value = false
  }
}

// 管理员登录跳转
const goToAdminLogin = () => {
  window.location.href = 'http://172.19.15.53:8080/#/login-admin'
}

// 前往注册页面
const goToRegister = () => {
  router.push('/register')
}

// ==================== 生命周期 ====================
onMounted(() => {
  floatingItems.value = createFloatingItems()
  startParallaxLoop()
})

onBeforeUnmount(() => {
  if (animationFrame) {
    cancelAnimationFrame(animationFrame)
  }
})
</script>

<style scoped>
.login-container {
  position: relative;
  width: 100%;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

/* 飘落图标 */
.floating-icon {
  position: absolute;
  color: rgba(255, 255, 255, 0.15);
  pointer-events: none;
  z-index: 0;
  will-change: transform;
}

/* 动画关键帧 */
@keyframes iconFloat {
  0%, 100% { margin-top: 0; }
  50% { margin-top: -25px; }
}

@keyframes iconDrift {
  0%, 100% { margin-left: 0; }
  25% { margin-left: 15px; }
  75% { margin-left: -15px; }
}

@keyframes iconRotate {
  0% { rotate: 0deg; }
  100% { rotate: 360deg; }
}

/* 光晕效果 */
.glow {
  position: absolute;
  width: 50%;
  height: 50%;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.12;
  pointer-events: none;
  z-index: 0;
}

.glow-1 {
  background: radial-gradient(circle, rgba(255,255,255,0.4), transparent 70%);
  top: -20%;
  left: -10%;
}

.glow-2 {
  background: radial-gradient(circle, rgba(200,180,255,0.3), transparent 70%);
  bottom: -20%;
  right: -10%;
}

.glow-3 {
  background: radial-gradient(circle, rgba(255,200,220,0.3), transparent 70%);
  top: 30%;
  right: 20%;
}

/* 主内容区 */
.login-content {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 60px;
  max-width: 1200px;
  width: 90%;
  z-index: 1;
}

/* 左侧欢迎区域 */
.login-left {
  color: #fff;
  flex: 1;
  max-width: 500px;
  animation: fadeInLeft 0.8s ease;
}

@keyframes fadeInLeft {
  from {
    opacity: 0;
    transform: translateX(-30px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.welcome-title h1 {
  font-size: 48px;
  margin-bottom: 12px;
  font-weight: 700;
  text-shadow: 0 2px 10px rgba(0,0,0,0.2);
}

.welcome-title p {
  font-size: 18px;
  opacity: 0.9;
  margin-bottom: 40px;
}

.feature-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 20px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  transition: transform 0.3s ease, background 0.3s ease;
}

.feature-item:hover {
  transform: translateX(8px);
  background: rgba(255, 255, 255, 0.2);
}

.feature-icon {
  font-size: 32px;
}

.feature-item h3 {
  font-size: 18px;
  margin-bottom: 4px;
}

.feature-item p {
  font-size: 13px;
  opacity: 0.8;
  margin: 0;
}

/* 登录卡片 */
.login-card {
  width: 420px;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  border-radius: 24px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  overflow: hidden;
  animation: fadeInRight 0.8s ease;
}

@keyframes fadeInRight {
  from {
    opacity: 0;
    transform: translateX(30px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.card-header {
  padding: 20px 24px 0;
  border-bottom: 1px solid #eee;
}

.card-header h2 {
  margin: 0;
  font-size: 24px;
  color: #333;
  font-weight: 600;
}

.card-body {
  padding: 30px 24px 36px;
}

.input-group {
  margin-bottom: 24px;
}

.input-group label {
  display: block;
  font-size: 14px;
  font-weight: 500;
  color: #333;
  margin-bottom: 8px;
}

.input-group input {
  width: 100%;
  padding: 12px 16px;
  border: 1px solid #e0e0e0;
  border-radius: 12px;
  font-size: 14px;
  transition: all 0.3s ease;
  box-sizing: border-box;
}

.input-group input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.password-wrapper {
  position: relative;
}

.password-wrapper input {
  padding-right: 45px;
}

.password-toggle {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  cursor: pointer;
  font-size: 18px;
  opacity: 0.6;
}

.password-toggle:hover {
  opacity: 1;
}

/* 登录按钮 */
.login-button {
  width: 100%;
  height: 48px;
  font-size: 16px;
  font-weight: 600;
  border-radius: 12px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
  color: white;
  cursor: pointer;
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
  transition: all 0.3s ease;
  margin-top: 12px;
}

.login-button:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 12px 25px rgba(102, 126, 234, 0.5);
}

.login-button:active:not(:disabled) {
  transform: translateY(0);
}

.login-button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.error-message {
  background: #fee2e2;
  color: #ef4444;
  padding: 10px 12px;
  border-radius: 8px;
  font-size: 13px;
  margin-bottom: 16px;
}

.register-tip {
  text-align: center;
  margin-top: 20px;
  font-size: 13px;
  color: #666;
}

.register-tip span {
  color: #667eea;
  cursor: pointer;
  font-weight: 500;
}

.register-tip span:hover {
  text-decoration: underline;
}

/* 管理员登录链接 */
.admin-link {
  text-align: center;
  margin-top: 16px;
  font-size: 13px;
  color: #999;
  cursor: pointer;
  transition: color 0.3s ease;
  padding: 8px;
  border-top: 1px solid #eee;
}

.admin-link:hover {
  color: #667eea;
}

/* Toast 通知 */
.global-toast {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 2000;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;
  border-radius: 40px;
  font-size: 14px;
  font-weight: 500;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

.global-toast.success {
  background: #10b981;
  color: white;
}

.global-toast.error {
  background: #ef4444;
  color: white;
}

.global-toast.warning {
  background: #f59e0b;
  color: white;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(-20px);
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
  
  .login-content {
    gap: 0;
  }
}
</style>