<template>
  <div class="app-container">
    <!-- 购物页面 -->
    <div v-if="currentTab === 'shop'" class="tab-content">
      <div class="user-top-bar">
        <div class="user-info">欢迎回来，{{ user?.username }}</div>
      </div>

      <!-- 搜索栏 -->
      <div class="search-bar">
        <input
          type="text"
          placeholder="搜索商品"
          v-model="keyword"
          @keyup.enter="loadGoods"
        />
        <select v-model="categoryId" @change="loadGoods">
          <option value="">全部分类</option>
          <option :value="c.id" v-for="c in categories" :key="c.id">
            {{ c.name }}
          </option>
        </select>
        <button @click="loadGoods">搜索</button>
      </div>

      <!-- 商品列表区域 -->
      <div class="goods-list" v-if="goodsList.length">
        <div class="goods-card" v-for="item in goodsList" :key="item.goodsId">
          <div class="card-content" @click="viewGoodsDetail(item)">
            <img 
              :src="getImageUrl(item.coverImage || item.imageUrl)" 
              alt="商品封面" 
              @error="handleImageError"
            />
            <div class="info">
              <h3>{{ item.title }}</h3>
              <p class="price">¥{{ item.price }}</p>
              <p class="seller">卖家：{{ item.sellerName || item.username || `用户${item.userId}` }}</p>
            </div>
          </div>
          <button class="buy-btn" @click="handleBuy(item)">立即购买</button>
        </div>
      </div>
      <div class="empty" v-else>暂无商品</div>

      <!-- 发布按钮 -->
      <button class="publish-btn" @click="showPublishModal = true">+</button>

      <!-- 发布商品弹窗 -->
      <div class="modal" v-if="showPublishModal" @click="closeModalOnBg">
        <div class="modal-content publish-modal" @click.stop>
          <div class="modal-header">
            <h3>发布商品</h3>
            <button class="close-modal-btn-icon" @click="closeModal">×</button>
          </div>
          
          <div class="form-section upload-section">
            <div class="section-title">商品图片 <span class="required">*</span></div>
            <div class="upload-area" @click="triggerFileInput">
              <input
                ref="fileInputRef"
                type="file"
                accept="image/*"
                @change="handleImageUpload"
                style="display: none"
              />
              <div v-if="!publishForm.previewImage" class="upload-placeholder">
                <div class="upload-icon">📷</div>
                <div class="upload-text">点击上传图片</div>
                <div class="upload-tip">支持 jpg、png 格式，不超过 5MB</div>
              </div>
              <div v-else class="upload-preview">
                <img :src="publishForm.previewImage" alt="预览" />
                <div class="upload-overlay">
                  <span class="reupload-btn">重新上传</span>
                </div>
              </div>
            </div>
            <div v-if="uploading" class="uploading-status">
              <span class="loading-spinner"></span> 正在上传中...
            </div>
          </div>

          <div class="form-section">
            <div class="section-title">基本信息</div>
            <div class="form-item">
              <label>商品名称 <span class="required">*</span></label>
              <input v-model="publishForm.title" placeholder="请输入商品名称" maxlength="100" />
            </div>
            <div class="form-item">
              <label>商品描述</label>
              <textarea v-model="publishForm.description" placeholder="请描述商品的成色、使用时长、功能状况等" rows="3"></textarea>
            </div>
          </div>

          <div class="form-row">
            <div class="form-item half">
              <label>价格（元）<span class="required">*</span></label>
              <input type="number" v-model.number="publishForm.price" placeholder="请输入价格" min="0" step="0.01" />
            </div>
            <div class="form-item half">
              <label>分类 <span class="required">*</span></label>
              <select v-model="publishForm.categoryId">
                <option value="" disabled>请选择分类</option>
                <option :value="c.id" v-for="c in categories" :key="c.id">
                  {{ c.name }}
                </option>
              </select>
            </div>
          </div>

          <div class="btn-group publish-btn-group">
            <button class="cancel-publish-btn" @click="closeModal">取消</button>
            <button class="submit-publish-btn" @click="handlePublish" :disabled="publishing">
              {{ publishing ? '发布中...' : '发布商品' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 我的页面 -->
    <div v-if="currentTab === 'mine'" class="tab-content mine-tab">
      <div class="user-info-card" @click="showUserInfoDialog = true">
        <h2>个人中心</h2>
        <p>用户名：{{ user?.username }}</p>
        <p>手机号：{{ user?.phone || "未填写" }}</p>
        <p v-if="user?.defaultLocation">默认地点：{{ user.defaultLocation }}</p>
        <p v-if="user?.defaultTimeType">默认交易时间：{{ user.defaultTimeType === 'immediate' ? '立即交易' : '约定时间' }}</p>
        <div class="edit-tip">点击此处修改个人信息 ✏️</div>
      </div>
      <div class="menu-list">
        <div class="menu-item" @click="openOrders">我的订单</div>
        <div class="menu-item" @click="openMyGoods">我发布的商品</div>
        <div class="menu-item" @click="showLocationDialog = true">设置默认交易信息</div>
        <div class="menu-item" @click="logout">退出登录</div>
      </div>
    </div>

    <!-- 修改个人信息弹窗 -->
    <div class="modal" v-if="showUserInfoDialog" @click="showUserInfoDialog = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>修改个人信息</h3>
          <button class="close-modal-btn-icon" @click="showUserInfoDialog = false">×</button>
        </div>
        <div class="form-item">
          <label>手机号</label>
          <input v-model="editPhone" type="tel" placeholder="请输入手机号" />
        </div>
        <div class="form-item">
          <label>默认交易地点</label>
          <input v-model="editLocation" placeholder="请输入默认交易地点" />
        </div>
        <div class="form-item">
          <label>默认交易时间</label>
          <select v-model="editTimeType">
            <option value="immediate">立即交易</option>
            <option value="custom">约定时间</option>
          </select>
        </div>
        <div class="btn-group">
          <button @click="showUserInfoDialog = false">取消</button>
          <button @click="saveUserInfo">保存</button>
        </div>
      </div>
    </div>

    <!-- 设置默认交易信息弹窗 -->
    <div class="modal" v-if="showLocationDialog" @click="showLocationDialog = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>设置默认交易信息</h3>
          <button class="close-modal-btn-icon" @click="showLocationDialog = false">×</button>
        </div>
        <div class="form-item">
          <label>交易地点</label>
          <input v-model="defaultLocation" placeholder="请输入常用交易地点" />
        </div>
        <div class="form-item">
          <label>默认交易时间</label>
          <select v-model="defaultTimeType">
            <option value="immediate">立即交易</option>
            <option value="custom">约定时间</option>
          </select>
        </div>
        <div class="btn-group">
          <button @click="showLocationDialog = false">取消</button>
          <button @click="saveDefaultInfo">保存</button>
        </div>
      </div>
    </div>

    <!-- 订单弹窗 -->
    <div class="modal" v-if="showOrderModal" @click="showOrderModal = false">
      <div class="modal-content order-modal" @click.stop>
        <div class="modal-header">
          <h3>我的订单</h3>
          <button class="close-modal-btn-icon" @click="showOrderModal = false">×</button>
        </div>
        <div v-if="orderLoading" class="loading-text">加载中...</div>
        <div v-else-if="!orders.length" class="empty-orders">暂无订单</div>
        <template v-else>
          <div class="orders-list">
            <div v-for="o in orders" :key="o.tradeId" class="order-item">
              <div class="order-info">
                <p><strong>商品：</strong>{{ o.goodsTitle || o.goods?.title }}</p>
                <p><strong>价格：</strong>¥{{ o.price }}</p>
                <p><strong>卖家：</strong>{{ o.sellerName || `用户${o.sellerId}` }}</p>
                <p><strong>交易地点：</strong>{{ o.meetLocation || '未填写' }}</p>
                <p><strong>交易时间：</strong>{{ formatMeetTime(o.meetTime) || '未填写' }}</p>
                <p><strong>状态：</strong>
                  <span :style="{ color: getOrderStatusColor(o.status) }">
                    {{ getOrderStatusText(o.status) }}
                  </span>
                </p>
              </div>
              <div class="order-actions">
                <button 
                  v-if="o.status === 0 && o.sellerId === user?.id" 
                  class="confirm-btn"
                  @click="confirmOrder(o.tradeId)"
                >
                  ✓ 确认交易
                </button>
                <button 
                  v-if="o.status === 0" 
                  class="cancel-btn"
                  @click="cancelOrder(o.tradeId)"
                >
                  ✗ 取消订单
                </button>
                <span v-if="o.status === 1" class="completed-badge">✅ 交易已完成</span>
                <span v-if="o.status === 2" class="cancelled-badge">❌ 交易已取消</span>
              </div>
            </div>
          </div>
        </template>
      </div>
    </div>

    <!-- 我发布的商品弹窗 -->
    <div class="modal" v-if="showMyGoodsModal" @click="showMyGoodsModal = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>我发布的商品</h3>
          <button class="close-modal-btn-icon" @click="showMyGoodsModal = false">×</button>
        </div>
        <div v-if="myGoodsLoading">加载中...</div>
        <div v-else-if="!myGoods.length">暂无商品</div>
        <template v-else>
          <div v-for="g in myGoods" :key="g.goodsId" class="order-item">
            <p>
              <strong>{{ g.title }}</strong> - ¥{{ g.price }}
              <span :style="{ fontSize: '12px', color: getGoodsStatusColor(g.audit_status || g.auditStatus), marginLeft: '8px' }">
                {{ getGoodsStatusText(g.audit_status || g.auditStatus) }}
              </span>
            </p>
            <button class="delete-btn" @click="handleDeleteGoods(g.goodsId)">下架</button>
          </div>
        </template>
      </div>
    </div>

    <!-- 商品详情弹窗 -->
    <div class="modal" v-if="showGoodsDetailModal" @click="closeGoodsDetail">
      <div class="modal-content goods-detail-modal" @click.stop>
        <div class="detail-header">
          <h3>{{ currentGoods?.title }}</h3>
          <button class="close-detail-btn" @click="closeGoodsDetail">×</button>
        </div>
        <div class="detail-content">
          <img 
            :src="getImageUrl(currentGoods?.coverImage || currentGoods?.imageUrl)" 
            alt="商品图片" 
            class="detail-img"
            @error="handleImageError"
          />
          <div class="detail-info">
            <p class="detail-price">¥{{ currentGoods?.price }}</p>
            <p class="detail-seller">卖家：{{ currentGoods?.sellerName || currentGoods?.username || `用户${currentGoods?.userId}` }}</p>
            <p class="detail-desc">{{ currentGoods?.description || '暂无商品描述' }}</p>
            <p class="detail-time">发布时间：{{ formatDate(currentGoods?.createTime) }}</p>
          </div>
        </div>
        <div class="detail-actions">
          <button class="buy-now-btn" @click="handleBuyFromDetail">立即购买</button>
          <button class="close-detail-action-btn" @click="closeGoodsDetail">关闭</button>
        </div>
      </div>
    </div>

    <!-- ========== 全局 Toast 通知 ========== -->
    <Transition name="toast">
      <div v-if="toastVisible" class="global-toast" :class="toastType">
        <span class="toast-icon">{{ toastIcon }}</span>
        <span class="toast-message">{{ toastMessage }}</span>
      </div>
    </Transition>

    <!-- ========== 购买确认弹窗（带交易时间） ========== -->
    <div class="modal" v-if="showBuyConfirmModal" @click="closeBuyConfirmModal">
      <div class="modal-content buy-confirm-modal" @click.stop>
        <div class="modal-header">
          <h3>确认购买</h3>
          <button class="close-modal-btn-icon" @click="closeBuyConfirmModal">×</button>
        </div>
        <div class="buy-confirm-content">
          <div class="buy-goods-preview">
            <img 
              :src="getImageUrl(buyTarget?.coverImage || buyTarget?.imageUrl)" 
              alt="商品图片"
              @error="handleImageError"
            />
            <div class="buy-goods-info">
              <h4>{{ buyTarget?.title }}</h4>
              <p class="buy-price">¥{{ buyTarget?.price }}</p>
              <p class="buy-seller">卖家：{{ buyTarget?.sellerName || buyTarget?.username || `用户${buyTarget?.userId}` }}</p>
            </div>
          </div>
          
          <!-- 交易地点 -->
          <div class="buy-location-section">
            <label>交易地点 <span class="required">*</span></label>
            <input 
              v-model="buyMeetLocation" 
              type="text"
              placeholder="请输入交易地点（如：学校南门、图书馆）"
            />
          </div>

          <!-- 交易时间 -->
          <div class="buy-time-section">
            <label>交易时间 <span class="required">*</span></label>
            <div class="time-options">
              <div class="time-option" 
                   :class="{ active: buyMeetTimeType === 'immediate' }"
                   @click="buyMeetTimeType = 'immediate'">
                <span class="time-option-icon">⏰</span>
                <div>
                  <div class="time-option-title">立即交易</div>
                  <div class="time-option-desc">下单后尽快完成交易</div>
                </div>
              </div>
              <div class="time-option" 
                   :class="{ active: buyMeetTimeType === 'custom' }"
                   @click="buyMeetTimeType = 'custom'">
                <span class="time-option-icon">📅</span>
                <div>
                  <div class="time-option-title">约定时间</div>
                  <div class="time-option-desc">选择具体交易时间</div>
                </div>
              </div>
            </div>
            
            <!-- 自定义时间选择 -->
            <div v-if="buyMeetTimeType === 'custom'" class="custom-time-picker">
              <div class="date-picker">
                <label>交易日期</label>
                <input type="date" v-model="buyMeetDate" :min="minDate" />
              </div>
              <div class="time-picker">
                <label>交易时间</label>
                <select v-model="buyMeetHour">
                  <option v-for="h in 24" :key="h-1" :value="h-1">
                    {{ String(h-1).padStart(2, '0') }}:00
                  </option>
                </select>
                <span class="time-separator">:</span>
                <select v-model="buyMeetMinute">
                  <option value="0">00</option>
                  <option value="30">30</option>
                </select>
              </div>
            </div>
          </div>

          <label class="remember-info">
            <input type="checkbox" v-model="rememberLocation" />
            <span>保存为默认交易信息</span>
          </label>
        </div>
        <div class="btn-group">
          <button class="cancel-btn" @click="closeBuyConfirmModal">取消</button>
          <button class="confirm-buy-btn" @click="confirmPurchase" :disabled="buying">
            {{ buying ? '处理中...' : '确认购买' }}
          </button>
        </div>
      </div>
    </div>

    <!-- 底部导航 -->
    <div class="bottom-nav">
      <div class="nav-item" :class="{ active: currentTab === 'shop' }" @click="currentTab = 'shop'">
        🛒 购物
      </div>
      <div class="nav-item" :class="{ active: currentTab === 'mine' }" @click="currentTab = 'mine'">
        👤 我的
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { goodsAPI, categoryAPI, tradeAPI, uploadAPI, clearToken } from '../api'

const router = useRouter()

// ==================== 响应式数据 ====================
const currentTab = ref('shop')
const user = ref(JSON.parse(localStorage.getItem('user') || '{}'))

const keyword = ref('')
const categoryId = ref('')
const goodsList = ref([])
const categories = ref([])

const showPublishModal = ref(false)
const publishing = ref(false)
const uploading = ref(false)
const fileInputRef = ref(null)

const publishForm = ref({
  title: '',
  description: '',
  price: 0,
  categoryId: '',
  imageUrls: [],
  previewImage: '',
  uploadedImageUrl: null
})

const showOrderModal = ref(false)
const orders = ref([])
const orderLoading = ref(false)

const showMyGoodsModal = ref(false)
const myGoods = ref([])
const myGoodsLoading = ref(false)

const showLocationDialog = ref(false)
const defaultLocation = ref(user.value.defaultLocation || '')
const defaultTimeType = ref(user.value.defaultTimeType || 'immediate')

// 修改个人信息
const showUserInfoDialog = ref(false)
const editPhone = ref(user.value.phone || '')
const editLocation = ref(user.value.defaultLocation || '')
const editTimeType = ref(user.value.defaultTimeType || 'immediate')

// 商品详情弹窗相关
const showGoodsDetailModal = ref(false)
const currentGoods = ref(null)

// ========== Toast 通知 ==========
const toastVisible = ref(false)
const toastMessage = ref('')
const toastType = ref('success')
let toastTimer = null

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

// ========== 购买确认弹窗 ==========
const showBuyConfirmModal = ref(false)
const buyTarget = ref(null)
const buyMeetLocation = ref('')
const buyMeetTimeType = ref('immediate')
const buyMeetDate = ref('')
const buyMeetHour = ref(12)
const buyMeetMinute = ref(0)
const rememberLocation = ref(false)
const buying = ref(false)

// 最小日期（今天）
const minDate = computed(() => {
  const today = new Date()
  return today.toISOString().split('T')[0]
})

const closeBuyConfirmModal = () => {
  showBuyConfirmModal.value = false
  buyTarget.value = null
  buyMeetLocation.value = ''
  buyMeetTimeType.value = 'immediate'
  buyMeetDate.value = ''
  buyMeetHour.value = 12
  buyMeetMinute.value = 0
  rememberLocation.value = false
}

// 格式化交易时间显示
const formatMeetTime = (timeStr) => {
  if (!timeStr) return ''
  try {
    const date = new Date(timeStr)
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
  } catch {
    return timeStr
  }
}

const confirmPurchase = async () => {
  if (!buyMeetLocation.value.trim()) {
    showToast('请输入交易地点', 'warning')
    return
  }
  
  let meetTime = null
  if (buyMeetTimeType.value === 'immediate') {
    meetTime = new Date().toISOString()
  } else {
    if (!buyMeetDate.value) {
      showToast('请选择交易日期', 'warning')
      return
    }
    meetTime = new Date(
      buyMeetDate.value + 'T' + 
      String(buyMeetHour.value).padStart(2, '0') + ':' + 
      String(buyMeetMinute.value).padStart(2, '0') + ':00'
    ).toISOString()
  }
  
  buying.value = true
  
  try {
    const userData = JSON.parse(localStorage.getItem('user') || '{}')
    const buyerId = userData.id || userData.userId || userData.user_id
    
    const params = {
      GoodsId: Number(buyTarget.value.goodsId),
      BuyerId: Number(buyerId),
      SellerId: Number(buyTarget.value.userId),
      MeetLocation: buyMeetLocation.value.trim(),
      MeetTime: meetTime
    }
    
    const res = await tradeAPI.create(params)
    
    if (res && res.success) {
      if (rememberLocation.value) {
        const updatedUser = { 
          ...userData, 
          defaultLocation: buyMeetLocation.value.trim(),
          defaultTimeType: buyMeetTimeType.value
        }
        localStorage.setItem('user', JSON.stringify(updatedUser))
        user.value = updatedUser
      }
      
      showToast('购买成功！请等待卖家确认', 'success')
      closeBuyConfirmModal()
      openOrders()
      loadGoods()
    } else {
      let errorMsg = res?.message || '购买失败，请稍后重试'
      if (errorMsg.includes('已对该商品发起过交易')) {
        errorMsg = '您已对该商品发起过交易，请等待卖家确认'
      } else if (errorMsg.includes('无效的商品ID')) {
        errorMsg = '商品不存在或已被删除'
      }
      showToast(errorMsg, 'error')
    }
  } catch (err) {
    console.error('购买异常:', err)
    showToast('网络异常，请稍后重试', 'error')
  } finally {
    buying.value = false
  }
}

// 后端 API 基础地址
const API_BASE_URL = 'http://172.19.229.149:5000'

// ==================== 辅助函数 ====================
const getOrderStatusText = (status) => {
  const statusMap = { 0: '待确认', 1: '已完成', 2: '已取消' }
  return statusMap[status] || '未知'
}

const getOrderStatusColor = (status) => {
  const colorMap = { 0: '#ff9800', 1: '#4caf50', 2: '#f44336' }
  return colorMap[status] || '#999'
}

const getGoodsStatusText = (status) => {
  const statusMap = { 0: '待审核', 1: '在售', 2: '已驳回' }
  return statusMap[status] || '未知'
}

const getGoodsStatusColor = (status) => {
  const colorMap = { 0: '#ff9800', 1: '#4caf50', 2: '#f44336' }
  return colorMap[status] || '#999'
}

// 获取完整图片 URL
const getImageUrl = (path) => {
  if (!path) return 'https://via.placeholder.com/200?text=暂无图片'
  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path
  }
  if (path.startsWith('/')) {
    return `${API_BASE_URL}${path}`
  }
  return `${API_BASE_URL}/${path}`
}

// 图片加载失败处理
const handleImageError = (e) => {
  e.target.src = 'https://via.placeholder.com/200?text=加载失败'
}

// 格式化日期
const formatDate = (dateStr) => {
  if (!dateStr) return '未知'
  try {
    const date = new Date(dateStr)
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
  } catch {
    return dateStr
  }
}

// ==================== 用户相关 ====================
const saveUserInfo = () => {
  const updatedUser = { 
    ...user.value, 
    phone: editPhone.value,
    defaultLocation: editLocation.value,
    defaultTimeType: editTimeType.value
  }
  localStorage.setItem('user', JSON.stringify(updatedUser))
  user.value = updatedUser
  showUserInfoDialog.value = false
  showToast('个人信息已更新', 'success')
}

const saveDefaultInfo = () => {
  const updatedUser = { 
    ...user.value, 
    defaultLocation: defaultLocation.value,
    defaultTimeType: defaultTimeType.value
  }
  localStorage.setItem('user', JSON.stringify(updatedUser))
  user.value = updatedUser
  showLocationDialog.value = false
  showToast('默认交易信息已保存', 'success')
}

const logout = () => {
  if (confirm('确定要退出登录吗？')) {
    clearToken()
    localStorage.removeItem('user')
    router.push('/login')
  }
}

// ==================== 分类相关 ====================
const loadCategories = async () => {
  try {
    const res = await categoryAPI.getList()
    if (res.success && Array.isArray(res.data)) {
      categories.value = res.data.map(item => ({
        id: item.categoryId,
        name: item.categoryName
      }))
    }
  } catch (err) {
    console.error('加载分类失败:', err)
  }
}

// ==================== 商品相关 ====================
// 获取卖家用户名
const fetchSellerNames = async (goodsList) => {
  for (const goods of goodsList) {
    if (!goods.sellerName && goods.userId) {
      try {
        if (goods.username) {
          goods.sellerName = goods.username
        } else {
          const userInfo = JSON.parse(localStorage.getItem('user') || '{}')
          if (userInfo.id === goods.userId) {
            goods.sellerName = userInfo.username
          } else {
            goods.sellerName = `用户${goods.userId}`
          }
        }
      } catch {
        goods.sellerName = `用户${goods.userId}`
      }
    }
  }
  return goodsList
}

const loadGoods = async () => {
  try {
    const params = {}
    if (keyword.value) params.keyword = keyword.value
    if (categoryId.value) params.categoryId = parseInt(categoryId.value)
    
    const res = await goodsAPI.getList(params)
    
    if (res.success && res.data) {
      let list = res.data.list || res.data
      list = await fetchSellerNames(Array.isArray(list) ? list : [])
      goodsList.value = list
    } else {
      goodsList.value = []
    }
  } catch (err) {
    console.error("加载商品失败:", err)
    goodsList.value = []
  }
}

const viewGoodsDetail = async (item) => {
  const originalItem = { ...item }
  currentGoods.value = { ...originalItem, description: '加载中...' }
  showGoodsDetailModal.value = true
  
  try {
    const res = await goodsAPI.getDetail(item.goodsId)
    
    if (res.success && res.data) {
      currentGoods.value = {
        ...originalItem,
        ...res.data,
        description: res.data.description || '暂无商品描述',
        createTime: res.data.createTime || originalItem.createTime,
        goodsId: originalItem.goodsId || res.data.goodsId,
        userId: originalItem.userId || res.data.userId,
        sellerName: originalItem.sellerName || res.data.username || `用户${originalItem.userId}`
      }
    } else {
      currentGoods.value = {
        ...originalItem,
        description: '暂无商品描述',
        sellerName: originalItem.sellerName || `用户${originalItem.userId}`
      }
    }
  } catch (err) {
    console.error('获取商品详情失败:', err)
    currentGoods.value = {
      ...originalItem,
      description: '获取描述失败',
      sellerName: originalItem.sellerName || `用户${originalItem.userId}`
    }
  }
}

const closeGoodsDetail = () => {
  showGoodsDetailModal.value = false
  currentGoods.value = null
}

const handleBuyFromDetail = () => {
  if (!currentGoods.value) {
    showToast('商品信息丢失，请重新打开详情', 'error')
    closeGoodsDetail()
    return
  }
  
  if (!currentGoods.value.goodsId) {
    showToast('商品ID丢失，请重新打开详情', 'error')
    closeGoodsDetail()
    return
  }
  
  if (!currentGoods.value.userId) {
    showToast('卖家信息丢失，请重新打开详情', 'error')
    closeGoodsDetail()
    return
  }
  
  const goodsToBuy = { ...currentGoods.value }
  closeGoodsDetail()
  handleBuy(goodsToBuy)
}

// 触发文件选择
const triggerFileInput = () => {
  if (publishForm.value.previewImage) return
  fileInputRef.value?.click()
}

// 图片上传预览
const handleImageUpload = async (e) => {
  const file = e.target.files[0]
  if (!file) return
  
  if (!file.type.startsWith('image/')) {
    showToast('请选择图片文件', 'warning')
    return
  }
  
  if (file.size > 5 * 1024 * 1024) {
    showToast('图片大小不能超过5MB', 'warning')
    return
  }
  
  const previewUrl = URL.createObjectURL(file)
  publishForm.value.previewImage = previewUrl
  uploading.value = true
  
  try {
    const uploadRes = await uploadAPI.upload(file)
    
    if (uploadRes.success) {
      publishForm.value.uploadedImageUrl = uploadRes.url
      showToast('图片上传成功', 'success')
    } else {
      URL.revokeObjectURL(previewUrl)
      publishForm.value.previewImage = ''
      publishForm.value.uploadedImageUrl = null
      showToast('图片上传失败：' + uploadRes.message, 'error')
    }
  } catch (err) {
    console.error('上传异常:', err)
    URL.revokeObjectURL(previewUrl)
    publishForm.value.previewImage = ''
    publishForm.value.uploadedImageUrl = null
    showToast('图片上传失败，请检查网络', 'error')
  } finally {
    uploading.value = false
    if (fileInputRef.value) {
      fileInputRef.value.value = ''
    }
  }
}

const closeModal = () => {
  showPublishModal.value = false
  if (publishForm.value.previewImage) {
    URL.revokeObjectURL(publishForm.value.previewImage)
  }
  publishForm.value = {
    title: '',
    description: '',
    price: 0,
    categoryId: '',
    imageUrls: [],
    previewImage: '',
    uploadedImageUrl: null
  }
}

const closeModalOnBg = () => {
  if (confirm('确定关闭？未发布的内容将丢失')) {
    closeModal()
  }
}

const handlePublish = async () => {
  if (!publishForm.value.title.trim()) {
    showToast('请填写商品名称', 'warning')
    return
  }
  if (!publishForm.value.categoryId) {
    showToast('请选择商品分类', 'warning')
    return
  }
  if (publishForm.value.price < 0) {
    showToast('价格不能为负数', 'warning')
    return
  }
  if (!publishForm.value.uploadedImageUrl) {
    showToast('请上传商品图片', 'warning')
    return
  }
  
  publishing.value = true
  
  try {
    const res = await goodsAPI.publish({
      title: publishForm.value.title.trim(),
      description: publishForm.value.description,
      price: publishForm.value.price,
      categoryId: parseInt(publishForm.value.categoryId),
      imageUrls: [publishForm.value.uploadedImageUrl]
    })
    
    if (res.success) {
      showToast('发布成功，等待审核', 'success')
      closeModal()
      loadGoods()
    } else {
      showToast(res.message || '发布失败', 'error')
    }
  } catch (err) {
    console.error('发布失败:', err)
    showToast('发布失败：' + (err.message || '网络错误'), 'error')
  } finally {
    publishing.value = false
  }
}

const handleDeleteGoods = async (id) => {
  if (!confirm('确定下架该商品？')) return
  
  try {
    const res = await goodsAPI.remove(id)
    
    if (res.success) {
      showToast('商品已下架', 'success')
      myGoods.value = myGoods.value.filter(g => g.goodsId !== id)
      loadGoods()
    } else {
      showToast('操作失败：' + (res.message || ''), 'error')
    }
  } catch (e) {
    console.error('下架失败:', e)
    showToast('操作失败：' + (e.message || '网络错误'), 'error')
  }
}

// ==================== 交易相关 ====================
const handleBuy = async (item) => {
  try {
    const userStr = localStorage.getItem('user')
    if (!userStr) {
      showToast('请先登录！', 'warning')
      router.push('/login')
      return
    }
    
    const userData = JSON.parse(userStr)
    const buyerId = userData.id || userData.userId || userData.user_id
    
    if (!buyerId) {
      showToast('用户信息不完整，请重新登录', 'error')
      router.push('/login')
      return
    }
    
    const goodsId = item.goodsId || item.id
    const sellerId = item.userId
    
    if (!goodsId || !sellerId) {
      showToast('商品信息不完整', 'error')
      return
    }
    
    if (Number(sellerId) === Number(buyerId)) {
      showToast('不能购买自己发布的商品', 'warning')
      return
    }
    
    // 显示购买确认弹窗
    buyTarget.value = item
    buyMeetLocation.value = userData.defaultLocation || ''
    buyMeetTimeType.value = userData.defaultTimeType || 'immediate'
    showBuyConfirmModal.value = true
  } catch (err) {
    console.error('购买异常:', err)
    showToast('网络异常，请检查网络连接后重试', 'error')
  }
}

const openOrders = async () => {
  showOrderModal.value = true
  orderLoading.value = true
  try {
    const res = await tradeAPI.getMyTrades()
    
    if (res.success && res.data) {
      let list = res.data.list || res.data
      list = list.map(order => ({
        ...order,
        tradeId: order.tradeId || order.trade_id,
        goodsId: order.goodsId || order.goods_id,
        goodsTitle: order.goodsTitle || order.goods?.title,
        price: order.price,
        meetLocation: order.meetLocation || order.MeetLocation,
        meetTime: order.meetTime,
        status: order.status,
        sellerId: order.sellerId || order.seller_id,
        buyerId: order.buyerId || order.buyer_id,
        sellerName: order.sellerName || `用户${order.sellerId || order.seller_id}`
      }))
      orders.value = Array.isArray(list) ? list : []
    } else {
      orders.value = []
    }
  } catch (err) {
    console.error('加载订单失败:', err)
    orders.value = []
  } finally {
    orderLoading.value = false
  }
}

const confirmOrder = async (tradeId) => {
  if (!confirm('确认这笔交易已完成？')) return
  
  try {
    const res = await tradeAPI.complete(tradeId)
    
    if (res && res.success) {
      showToast('交易确认成功！', 'success')
      await openOrders()
      await loadGoods()
      if (showMyGoodsModal.value) {
        await openMyGoods()
      }
    } else {
      showToast(res?.message || '确认失败，请稍后重试', 'error')
    }
  } catch (err) {
    console.error('确认订单失败:', err)
    showToast('网络异常，请稍后重试', 'error')
  }
}

const cancelOrder = async (tradeId) => {
  if (!confirm('确定要取消这笔交易吗？')) return
  
  try {
    const res = await tradeAPI.cancel(tradeId)
    
    if (res && res.success) {
      showToast('订单已取消', 'success')
      openOrders()
      loadGoods()
    } else {
      showToast(res?.message || '取消失败，请稍后重试', 'error')
    }
  } catch (err) {
    console.error('取消订单失败:', err)
    showToast('网络异常，请稍后重试', 'error')
  }
}

const openMyGoods = async () => {
  showMyGoodsModal.value = true
  myGoodsLoading.value = true
  try {
    const res = await goodsAPI.getMyGoods()
    
    if (res.success && res.data) {
      const list = res.data.list || res.data
      myGoods.value = Array.isArray(list) ? list : []
    } else {
      myGoods.value = []
    }
  } catch (err) {
    console.error('加载我的商品失败:', err)
    myGoods.value = []
  } finally {
    myGoodsLoading.value = false
  }
}

// ==================== 生命周期 ====================
onMounted(() => {
  loadCategories()
  loadGoods()
})
</script>

<style scoped>
.app-container {
  width: 100%;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f5f7fa;
  padding-bottom: 60px;
}
.tab-content { padding: 12px; }
.user-top-bar {
  background: #fff;
  padding: 10px 14px;
  border-radius: 8px;
  margin-bottom: 10px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  box-shadow: 0 1px 2px rgba(0,0,0,0.05);
}
.search-bar {
  display: flex;
  gap: 0;
  margin-bottom: 12px;
}
.search-bar input { flex:1; height:38px; padding:0 12px; border:1px solid #ddd; border-radius:4px 0 0 4px; outline:none; }
.search-bar input:focus { border-color: #007bff; }
.search-bar select { width:110px; height:38px; border:1px solid #ddd; border-left:none; border-right:none; outline:none; }
.search-bar button { width:70px; height:38px; border:none; background:#007bff; color:white; cursor:pointer; border-radius:0 4px 4px 0; }
.goods-list { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; }
.goods-card { background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
.card-content { cursor: pointer; transition: opacity 0.2s; }
.card-content:hover { opacity: 0.85; }
.goods-card img { width:100%; height:120px; object-fit: contain; background: #f5f5f5; }
.info { padding:8px; }
.info h3 { font-size:14px; margin:0 0 4px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
.price { color:#e4393c; font-weight:bold; font-size:16px; margin:4px 0; }
.seller { font-size:12px; color:#666; margin:4px 0; }
.buy-btn { width:100%; background:#007bff; color:white; border:none; padding:8px 0; cursor:pointer; transition:background 0.2s; }
.buy-btn:hover { background:#0056b3; }
.empty { text-align:center; color:#999; margin-top:40px; padding:40px; }
.publish-btn { position:fixed; right:20px; bottom:80px; width:50px; height:50px; border-radius:50%; background:#007bff; color:white; font-size:24px; border:none; cursor:pointer; box-shadow:0 2px 8px rgba(0,0,0,0.2); z-index:100; transition:transform 0.2s; }
.publish-btn:hover { transform:scale(1.05); background:#0056b3; }

/* 模态框通用样式 */
.modal { position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); display:flex; align-items:center; justify-content:center; z-index:1000; }
.modal-content { background:white; border-radius:16px; width:90%; max-width:450px; max-height:85vh; overflow-y:auto; }

/* 弹窗头部 */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #eee;
}
.modal-header h3 {
  margin: 0;
  font-size: 18px;
}
.close-modal-btn-icon {
  width: 32px;
  height: 32px;
  border: none;
  background: none;
  font-size: 24px;
  cursor: pointer;
  color: #999;
  border-radius: 50%;
  transition: all 0.2s;
}
.close-modal-btn-icon:hover {
  background: #f0f0f0;
  color: #333;
}

/* 表单区块 */
.form-section {
  padding: 0 20px;
  margin-bottom: 20px;
}
.section-title {
  font-weight: 600;
  font-size: 15px;
  color: #333;
  margin-bottom: 12px;
}
.required {
  color: #e4393c;
  margin-left: 2px;
}

/* 图片上传区域 */
.upload-section {
  background: #f8f9fa;
  padding: 20px;
  margin-bottom: 0;
  border-bottom: 8px solid #f0f0f0;
}
.upload-area {
  cursor: pointer;
}
.upload-placeholder {
  border: 2px dashed #ddd;
  border-radius: 12px;
  padding: 40px 20px;
  text-align: center;
  transition: all 0.2s;
  background: #fff;
}
.upload-placeholder:hover {
  border-color: #007bff;
  background: #f0f7ff;
}
.upload-icon {
  font-size: 48px;
  margin-bottom: 12px;
}
.upload-text {
  color: #333;
  margin-bottom: 8px;
}
.upload-tip {
  font-size: 12px;
  color: #999;
}
.upload-preview {
  position: relative;
  border-radius: 12px;
  overflow: hidden;
}
.upload-preview img {
  width: 100%;
  height: 200px;
  object-fit: contain;
  background: #f5f5f5;
  display: block;
}
.upload-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s;
}
.upload-preview:hover .upload-overlay {
  opacity: 1;
}
.reupload-btn {
  background: rgba(0,0,0,0.7);
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  font-size: 14px;
}
.uploading-status {
  margin-top: 12px;
  text-align: center;
  color: #007bff;
  font-size: 13px;
}
.loading-spinner {
  display: inline-block;
  width: 14px;
  height: 14px;
  border: 2px solid #007bff;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
  margin-right: 6px;
  vertical-align: middle;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}

/* 表单项 */
.form-item {
  margin-bottom: 16px;
}
.form-item label {
  display: block;
  margin-bottom: 6px;
  font-weight: 500;
  font-size: 14px;
}
.form-item input, 
.form-item textarea, 
.form-item select { 
  width: 100%; 
  padding: 10px 12px; 
  border: 1px solid #ddd; 
  border-radius: 8px; 
  font-size: 14px; 
  box-sizing: border-box; 
  transition: border-color 0.2s;
}
.form-item input:focus, 
.form-item textarea:focus, 
.form-item select:focus { 
  outline: none; 
  border-color: #007bff; 
}

/* 双栏布局 */
.form-row {
  display: flex;
  gap: 12px;
  padding: 0 20px;
  margin-bottom: 20px;
}
.form-item.half {
  flex: 1;
  margin-bottom: 0;
}

/* 按钮组 */
.btn-group {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 20px;
  border-top: 1px solid #eee;
}
.publish-btn-group {
  padding-top: 20px;
}
.cancel-publish-btn {
  flex: 1;
  padding: 12px;
  background: #f0f0f0;
  color: #666;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 15px;
  transition: background 0.2s;
}
.cancel-publish-btn:hover {
  background: #e0e0e0;
}
.submit-publish-btn {
  flex: 1;
  padding: 12px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 15px;
  transition: background 0.2s;
}
.submit-publish-btn:hover:not(:disabled) {
  background: #0056b3;
}
.submit-publish-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* 订单弹窗 */
.order-modal { max-width: 450px; }
.orders-list { max-height: 60vh; overflow-y: auto; padding: 0 20px; }
.loading-text, .empty-orders { text-align:center; padding:40px; color:#999; }

/* 个人中心 */
.user-info-card { 
  background: white; 
  padding: 20px; 
  border-radius: 12px; 
  margin-bottom: 20px;
  cursor: pointer;
  transition: all 0.2s;
  position: relative;
}
.user-info-card:hover {
  background: #f8f9fa;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}
.user-info-card h2 { margin:0 0 12px; font-size:18px; }
.user-info-card p { margin:8px 0; color:#666; }
.edit-tip {
  margin-top: 12px;
  font-size: 12px;
  color: #007bff;
  text-align: center;
  border-top: 1px dashed #eee;
  padding-top: 10px;
}
.menu-list { background:white; border-radius:12px; overflow:hidden; }
.menu-item { padding:14px 20px; border-bottom:1px solid #eee; cursor:pointer; transition:background 0.2s; }
.menu-item:hover { background:#f5f5f5; }

/* 底部导航 */
.bottom-nav { position:fixed; bottom:0; left:0; width:100%; height:55px; background:white; border-top:1px solid #eee; display:flex; z-index:100; }
.nav-item { flex:1; display:flex; align-items:center; justify-content:center; color:#666; cursor:pointer; font-size:14px; transition:color 0.2s; }
.nav-item.active { color:#007bff; font-weight:bold; }

/* 订单卡片 */
.order-item { padding:12px 0; border-bottom:1px solid #eee; }
.order-info p { margin:6px 0; font-size:14px; }
.order-actions { display:flex; gap:10px; margin-top:12px; flex-wrap:wrap; }
.confirm-btn { background:#4caf50; color:white; border:none; padding:6px 16px; border-radius:4px; cursor:pointer; transition:background 0.2s; }
.confirm-btn:hover { background:#45a049; }
.cancel-btn { background:#ff9800; color:white; border:none; padding:6px 16px; border-radius:4px; cursor:pointer; transition:background 0.2s; }
.cancel-btn:hover { background:#e68900; }
.completed-badge { color:#4caf50; font-size:13px; padding:4px 8px; background:#e8f5e9; border-radius:4px; }
.cancelled-badge { color:#f44336; font-size:13px; padding:4px 8px; background:#ffebee; border-radius:4px; }
.delete-btn { margin-top:8px; padding:4px 12px; background:#dc3545; color:white; border:none; border-radius:4px; cursor:pointer; transition:background 0.2s; }
.delete-btn:hover { background:#c82333; }

/* 商品详情弹窗 */
.goods-detail-modal {
  max-width: 500px;
  max-height: 85vh;
  overflow-y: auto;
}
.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #eee;
}
.detail-header h3 {
  margin: 0;
  font-size: 18px;
}
.close-detail-btn {
  width: 32px;
  height: 32px;
  border: none;
  background: none;
  font-size: 24px;
  cursor: pointer;
  color: #999;
  border-radius: 50%;
}
.close-detail-btn:hover {
  background: #f0f0f0;
  color: #333;
}
.detail-content {
  padding: 20px;
}
.detail-img {
  width: 100%;
  max-height: 300px;
  object-fit: contain;
  background: #f5f5f5;
  border-radius: 8px;
  margin-bottom: 16px;
}
.detail-info {
  margin-bottom: 20px;
}
.detail-price {
  font-size: 24px;
  font-weight: bold;
  color: #e4393c;
  margin: 8px 0;
}
.detail-seller {
  color: #666;
  font-size: 14px;
  margin: 8px 0;
}
.detail-desc {
  color: #333;
  line-height: 1.5;
  margin: 12px 0;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 8px;
}
.detail-time {
  color: #999;
  font-size: 12px;
  margin: 8px 0;
}
.detail-actions {
  display: flex;
  gap: 12px;
  padding: 16px 20px;
  border-top: 1px solid #eee;
}
.buy-now-btn {
  flex: 1;
  padding: 10px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  transition: background 0.2s;
}
.buy-now-btn:hover {
  background: #0056b3;
}
.close-detail-action-btn {
  flex: 1;
  padding: 10px;
  background: #6c757d;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  transition: background 0.2s;
}
.close-detail-action-btn:hover {
  background: #5a6268;
}

/* ========== Toast 通知样式 ========== */
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

.global-toast.info {
  background: #3b82f6;
  color: white;
}

.toast-icon {
  font-size: 16px;
  font-weight: bold;
}

.toast-message {
  max-width: 280px;
  word-break: break-word;
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

/* ========== 购买确认弹窗样式 ========== */
.buy-confirm-modal {
  max-width: 420px;
}

.buy-confirm-content {
  padding: 20px;
}

.buy-goods-preview {
  display: flex;
  gap: 16px;
  padding-bottom: 16px;
  margin-bottom: 16px;
  border-bottom: 1px solid #eee;
}

.buy-goods-preview img {
  width: 80px;
  height: 80px;
  object-fit: contain;
  background: #f5f5f5;
  border-radius: 8px;
}

.buy-goods-info {
  flex: 1;
}

.buy-goods-info h4 {
  margin: 0 0 8px;
  font-size: 16px;
}

.buy-price {
  color: #e4393c;
  font-size: 18px;
  font-weight: bold;
  margin: 4px 0;
}

.buy-seller {
  color: #666;
  font-size: 12px;
  margin: 0;
}

.buy-location-section {
  margin-bottom: 16px;
}

.buy-location-section label {
  display: block;
  font-weight: 500;
  font-size: 14px;
  margin-bottom: 8px;
}

.buy-location-section input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  box-sizing: border-box;
}

.buy-location-section input:focus {
  outline: none;
  border-color: #007bff;
}

/* 交易时间选择 */
.buy-time-section {
  margin-bottom: 16px;
}

.buy-time-section label {
  display: block;
  font-weight: 500;
  font-size: 14px;
  margin-bottom: 8px;
}

.time-options {
  display: flex;
  gap: 12px;
  margin-bottom: 12px;
}

.time-option {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.time-option:hover {
  border-color: #007bff;
  background: #f0f7ff;
}

.time-option.active {
  border-color: #007bff;
  background: #e8f4ff;
}

.time-option-icon {
  font-size: 20px;
}

.time-option-title {
  font-weight: 500;
  font-size: 13px;
}

.time-option-desc {
  font-size: 11px;
  color: #999;
}

.custom-time-picker {
  margin-top: 12px;
  padding: 12px;
  background: #f8f9fa;
  border-radius: 8px;
}

.date-picker,
.time-picker {
  margin-bottom: 8px;
}

.date-picker label,
.time-picker label {
  font-size: 12px;
  margin-bottom: 4px;
  color: #666;
}

.date-picker input {
  width: 100%;
  padding: 8px 10px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 13px;
}

.time-picker {
  display: flex;
  align-items: center;
  gap: 5px;
  flex-wrap: wrap;
}

.time-picker label {
  width: 100%;
  margin-bottom: 4px;
}

.time-picker select {
  flex: 1;
  padding: 8px 10px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 13px;
}

.time-separator {
  font-size: 14px;
  font-weight: bold;
}

.remember-info {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #666;
  cursor: pointer;
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px solid #eee;
}

.remember-info input {
  width: auto;
}

.confirm-buy-btn {
  flex: 1;
  padding: 12px;
  background: #e4393c;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 15px;
  transition: background 0.2s;
}

.confirm-buy-btn:hover:not(:disabled) {
  background: #c8232c;
}

.confirm-buy-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>