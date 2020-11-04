using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Prisma.Diagnostics.Logging;
using Prisma.Graphics;
using Prisma.Natives.SDL;
using Vulkan;

namespace Prisma.System
{
    internal delegate void LowLevelDrawDelegate(
        Image[] images,
        Framebuffer[] framebuffers,
        RenderPass renderPass,
        SurfaceCapabilitiesKhr surfaceCapabilities,
        CommandBuffer buffer,
        DrawDelegate drawDelegate
    );
        
    public class GraphicsManager
    {
        private readonly Log _log;

        private Game _game;

        private Device _device;
        private Queue _queue;
        private SwapchainKhr _swapchainKhr;
        private Semaphore _semaphore;
        private SurfaceCapabilitiesKhr _surfaceCapabilities;
        private RenderPass _renderPass;

        private Fence _fence;
        private Image[] _images;
        private Framebuffer[] _framebuffers;
        private CommandBuffer[] _commandBuffers;

        internal Instance VulkanInstance { get; set; }

        internal IntPtr VulkanInstanceHandle =>
            (IntPtr)typeof(Instance)
                ?.GetField("m", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(VulkanInstance);

        internal SurfaceKhr SdlVulkanSurface { get; set; }
        internal UIntPtr SdlVulkanSurfaceHandle { get; set; }

        internal IntPtr SdlRendererHandle { get; set; }
        internal RenderContext RenderContext { get; set; }

        internal GraphicsManager(Game game, IntPtr sdlRendererHandle)
        {
            _log = LogManager.GetForCurrentAssembly();
            _game = game;

            SdlRendererHandle = sdlRendererHandle;

            CreateVulkanInstance();

            SDL2.SDL_Vulkan_CreateSurface(
                _game.Window.SdlWindowHandle,
                VulkanInstanceHandle,
                out var surfacePtr
            );

            SdlVulkanSurfaceHandle = new UIntPtr(surfacePtr);
            SdlVulkanSurface = (SurfaceKhr)typeof(SurfaceKhr).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, Type.EmptyTypes, null
            ).Invoke(null);
            typeof(SurfaceKhr).GetField("m", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(SdlVulkanSurface, surfacePtr
            );

            var physDevs = VulkanInstance.EnumeratePhysicalDevices();
            InitializeVulkan(physDevs[0], SdlVulkanSurface);

            RenderContext = new RenderContext(game);
        }

        internal void DrawFrame(DrawDelegate drawDelegate)
        {
            var nextIndex = _device.AcquireNextImageKHR(_swapchainKhr, ulong.MaxValue, _semaphore);

            _device.ResetFence(_fence);
            
            var submitInfo = new SubmitInfo
            {
                WaitSemaphores = new[] {_semaphore},
                WaitDstStageMask = new[] {PipelineStageFlags.AllGraphics},
                CommandBuffers = new[] {_commandBuffers[nextIndex]}
            };

            _queue.Submit(submitInfo, _fence);
            
            _device.WaitForFence(_fence, true, 100000000);
            var presentInfo = new PresentInfoKhr
            {
                Swapchains = new[] {_swapchainKhr},
                ImageIndices = new[] {nextIndex}
            };
            
            _queue.PresentKHR(presentInfo);
        }

        private void CreateVulkanInstance()
        {
            var engineVer = Assembly.GetExecutingAssembly().GetName().Version;

            var layerProperties = Commands.EnumerateInstanceLayerProperties();
            var layersToEnable = layerProperties.Any(l => l.LayerName == "VK_LAYER_LUNARG_standard_validation")
                ? new[] {"VK_LAYER_LUNARG_standard_validation"}
                : new string [0];

            SDL2.SDL_Vulkan_GetInstanceExtensions(
                _game.Window.SdlWindowHandle,
                out var count,
                null
            );

            var ptrs = new IntPtr[count];

            var result = SDL2.SDL_Vulkan_GetInstanceExtensions(
                _game.Window.SdlWindowHandle,
                out count,
                ptrs
            );

            List<string> extStrings;

            if (result == SDL2.SDL_bool.SDL_TRUE)
            {
                extStrings = ptrs.Select(
                    x => Marshal.PtrToStringUTF8(x)
                ).ToList();
            }
            else
            {
                _log.Warning("Failed to enumerate extensions for SDL. Adding defaults instead.");
                extStrings = new List<string>
                {
                    "VK_KHR_surface"
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    extStrings.Add("VK_KHR_win32_surface");
                }
            }

#if DEBUG
            extStrings.Add("VK_EXT_debug_report");
#endif
            VulkanInstance = new Instance(new InstanceCreateInfo
            {
                EnabledExtensionNames = extStrings.ToArray(),
                EnabledLayerNames = layersToEnable,

                ApplicationInfo = new ApplicationInfo()
                {
                    ApiVersion = Vulkan.Version.Make(1, 0, 0),
                    EngineName = "Prisma Engine",
                    EngineVersion = Vulkan.Version.Make(
                        (uint)engineVer.Major,
                        (uint)engineVer.Minor,
                        (uint)engineVer.Build
                    ),
                    ApplicationName = string.Empty,
                    ApplicationVersion = Vulkan.Version.Make(
                        (uint)_game.Version.Major,
                        (uint)_game.Version.Minor,
                        (uint)_game.Version.Build
                    )
                }
            });

#if DEBUG
            VulkanInstance.EnableDebug(DebugCallback);
#endif
        }

        private void InitializeVulkan(PhysicalDevice physDev, SurfaceKhr surface)
        {
            var queueFamilyProperties = physDev.GetQueueFamilyProperties();
            uint queueFamilyUsedIndex;

            for (queueFamilyUsedIndex = 0; queueFamilyUsedIndex < queueFamilyProperties.Length; ++queueFamilyUsedIndex)
            {
                if (!physDev.GetSurfaceSupportKHR(queueFamilyUsedIndex, surface)) continue;
                if (queueFamilyProperties[queueFamilyUsedIndex].QueueFlags.HasFlag(QueueFlags.Graphics)) break;
            }

            var queueInfo = new DeviceQueueCreateInfo
            {
                QueuePriorities = new[] {1.0f},
                QueueFamilyIndex = queueFamilyUsedIndex
            };

            var deviceInfo = new DeviceCreateInfo
            {
                EnabledExtensionNames = new[]
                {
                    "VK_KHR_swapchain",
                },
                QueueCreateInfos = new[] {queueInfo}
            };
            _device = physDev.CreateDevice(deviceInfo);

            _queue = _device.GetQueue(0, 0);
            _surfaceCapabilities = physDev.GetSurfaceCapabilitiesKHR(surface);
            var surfaceFormat = SelectSurfaceFormat(physDev, surface);
            _swapchainKhr = CreateSwapchainKhr(surface, surfaceFormat);
            _images = _device.GetSwapchainImagesKHR(_swapchainKhr);
            _renderPass = CreateRenderPass(surfaceFormat);
            _framebuffers = CreateFramebuffers(_images, surfaceFormat);
            var fenceInfo = new FenceCreateInfo();
            _fence = _device.CreateFence(fenceInfo);
            var semaphoreInfo = new SemaphoreCreateInfo();
            _semaphore = _device.CreateSemaphore(semaphoreInfo);
            _commandBuffers = CreateCommandBuffers(_images, _framebuffers, _renderPass, _surfaceCapabilities);
        }

        private SurfaceFormatKhr SelectSurfaceFormat(PhysicalDevice physDev, SurfaceKhr surface)
        {
            foreach (var f in physDev.GetSurfaceFormatsKHR(surface))
                if (f.Format == Format.R8G8B8A8Unorm || f.Format == Format.B8G8R8A8Unorm)
                    return f;

            throw new EngineException(
                "didn't find the R8G8B8A8Unorm or B8G8R8A8Unorm format",
                string.Empty
            );
        }

        private SwapchainKhr CreateSwapchainKhr(SurfaceKhr surface, SurfaceFormatKhr surfaceFormat)
        {
            var compositeAlpha = _surfaceCapabilities.SupportedCompositeAlpha.HasFlag(CompositeAlphaFlagsKhr.Inherit)
                ? CompositeAlphaFlagsKhr.Inherit
                : CompositeAlphaFlagsKhr.Opaque;

            var swapchainInfo = new SwapchainCreateInfoKhr
            {
                Surface = surface,
                MinImageCount = _surfaceCapabilities.MinImageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = _surfaceCapabilities.CurrentExtent,
                ImageUsage = ImageUsageFlags.ColorAttachment,
                PreTransform = SurfaceTransformFlagsKhr.Identity,
                ImageArrayLayers = 1,
                ImageSharingMode = SharingMode.Exclusive,
                QueueFamilyIndices = new uint[] {0},
                PresentMode = PresentModeKhr.Fifo,
                CompositeAlpha = compositeAlpha
            };

            return _device.CreateSwapchainKHR(swapchainInfo);
        }

        private Framebuffer[] CreateFramebuffers(Image[] images, SurfaceFormatKhr surfaceFormat)
        {
            var displayViews = new ImageView[images.Length];

            for (var i = 0; i < images.Length; i++)
            {
                var viewCreateInfo = new ImageViewCreateInfo
                {
                    Image = images[i],
                    ViewType = ImageViewType.View2D,
                    Format = surfaceFormat.Format,
                    Components = new ComponentMapping
                    {
                        R = ComponentSwizzle.R,
                        G = ComponentSwizzle.G,
                        B = ComponentSwizzle.B,
                        A = ComponentSwizzle.A
                    },
                    SubresourceRange = new ImageSubresourceRange
                    {
                        AspectMask = ImageAspectFlags.Color,
                        LevelCount = 1,
                        LayerCount = 1
                    }
                };

                displayViews[i] = _device.CreateImageView(viewCreateInfo);
            }

            var framebuffers = new Framebuffer [images.Length];

            for (var i = 0; i < images.Length; i++)
            {
                var frameBufferCreateInfo = new FramebufferCreateInfo
                {
                    Layers = 1,
                    RenderPass = _renderPass,
                    Attachments = new[] {displayViews[i]},
                    Width = _surfaceCapabilities.CurrentExtent.Width,
                    Height = _surfaceCapabilities.CurrentExtent.Height
                };

                framebuffers[i] = _device.CreateFramebuffer(frameBufferCreateInfo);
            }

            return framebuffers;
        }

        private RenderPass CreateRenderPass(SurfaceFormatKhr surfaceFormat)
        {
            var attDesc = new AttachmentDescription
            {
                Format = surfaceFormat.Format,
                Samples = SampleCountFlags.Count1,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            };

            var attRef = new AttachmentReference {Layout = ImageLayout.ColorAttachmentOptimal};

            var subpassDesc = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachments = new[] {attRef}
            };

            var renderPassCreateInfo = new RenderPassCreateInfo
            {
                Attachments = new[] {attDesc},
                Subpasses = new[] {subpassDesc}
            };

            return _device.CreateRenderPass(renderPassCreateInfo);
        }

        private CommandBuffer[] CreateCommandBuffers(
            Image[] images,
            Framebuffer[] framebuffers,
            RenderPass renderPass,
            SurfaceCapabilitiesKhr surfaceCapabilities)
        {
            var createPoolInfo = new CommandPoolCreateInfo {Flags = CommandPoolCreateFlags.ResetCommandBuffer};
            var commandPool = _device.CreateCommandPool(createPoolInfo);
            var commandBufferAllocateInfo = new CommandBufferAllocateInfo
            {
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = (uint)images.Length
            };
            var buffers = _device.AllocateCommandBuffers(commandBufferAllocateInfo);
            for (var i = 0; i < images.Length; i++)
            {
                var commandBufferBeginInfo = new CommandBufferBeginInfo();
                buffers[i].Begin(commandBufferBeginInfo);

                var renderPassBeginInfo = new RenderPassBeginInfo
                {
                    Framebuffer = framebuffers[i],
                    RenderPass = renderPass,
                    ClearValues = new[]
                    {
                        new ClearValue
                        {
                            Color = new ClearColorValue(new[]
                            {
                                255f / RenderContext.ClearColor.R,
                                255f / RenderContext.ClearColor.G,
                                255f / RenderContext.ClearColor.B,
                                255f / RenderContext.ClearColor.A
                            })
                        }
                    },
                    RenderArea = new Rect2D {Extent = surfaceCapabilities.CurrentExtent}
                };
                buffers[i].CmdBeginRenderPass(renderPassBeginInfo, SubpassContents.Inline);

                RenderContext.FillCommandBuffer(buffers[i]);
                
                buffers[i].CmdEndRenderPass();
                buffers[i].End();
            }

            return buffers;
        }

#if DEBUG
        private Bool32 DebugCallback(DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, ulong objectHandle,
            IntPtr location, int messageCode, IntPtr layerPrefix, IntPtr message, IntPtr userData)
        {
            Debug.WriteLine($"{flags}: {Marshal.PtrToStringAnsi(message)}");
            return true;
        }
#endif
    }
}