// using Microsoft.AspNetCore.Mvc;
// namespace ProductAPI;

// // annotation
// [ApiController]
// [Route("api/[controller]")]
// public class ProductController : ControllerBase
// {
//     //Data
//     private static readonly List<Product> products = new List<Product>
//     {
//         new Product { Id = 1, Name = "Laptop", Price = 999.99m, Description = "A high-performance laptop for work and play.", Category = "Electronics", ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?q=80&w=2071&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D" },
//         new Product { Id = 2, Name = "Smartphone", Price = 499.99m, Description = "A sleek smartphone with the latest features.", Category = "Electronics", ImageUrl = "https://images.unsplash.com/photo-1555774698-0b77e0d5fac6?q=80&w=2340&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D" },
//         new Product { Id = 3, Name = "Headphones", Price = 199.99m, Description = "Noise-cancelling headphones for immersive sound.", Category = "Audio", ImageUrl = "https://plus.unsplash.com/premium_photo-1679513691474-73102089c117?q=80&w=2013&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D" },
//         new Product { Id = 4, Name = "Coffee Maker", Price = 79.99m, Description = "Brew the perfect cup of coffee every morning.", Category = "Home Appliances", ImageUrl = "https://images.unsplash.com/photo-1565452344518-47faca79dc69?q=80&w=1335&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D" },
//         new Product { Id = 5, Name = "Gaming Console", Price = 399.99m, Description = "Experience next-gen gaming with stunning graphics.", Category = "Gaming", ImageUrl = "https://images.unsplash.com/photo-1580234797602-22c37b2a6230?q=80&w=2334&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D" }
//     };

//     [HttpGet()]
//     [Route("getall")]
//     public ActionResult<List<Product>> Get()
//     {
//         return Ok(products);
//     }

//     [HttpGet("{id}")] //2
//     public ActionResult<Product> Get(int id) //2
//     {
//         var product = products.FirstOrDefault(p => p.Id == id);   //p.Id = 2  == 2
//         if (product == null)
//         {
//             return NotFound(
//                 "Product not found with ID: " + id
//             );
//         }
//         return Ok(product);
//     }

//     [HttpPost]
//     public ActionResult<Product> Post(Product product)
//     {
//         product.Id = products.Max(p => p.Id) + 1; // Auto-increment ID
//         products.Add(product);
//         return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
//     }
//     [HttpPut("{id}")]//6
//     public ActionResult Put(int id, Product product)//6
//     {
//         var existingProduct = products.FirstOrDefault(p => p.Id == id);
//         if (existingProduct == null)
//         {
//             return NotFound();
//         }
//         products.Remove(existingProduct);
//         product.Id = id;
//         products.Add(product);
//         return Ok(product);
//     }
//     [HttpDelete("{id}")]
//     public ActionResult Delete(int id)
//     {
//         var product = products.FirstOrDefault(p => p.Id == id);
//         if (product == null)
//         {
//             return NotFound();
//         }
//         products.Remove(product);
//         return NoContent();
//     }
// }
