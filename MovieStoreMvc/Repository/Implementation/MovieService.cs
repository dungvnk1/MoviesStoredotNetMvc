using MovieStoreMvc.Models.Domain;
using MovieStoreMvc.Models.DTO;
using MovieStoreMvc.Repository.Abstract;

namespace MovieStoreMvc.Repository.Implementation
{
    public class MovieService : IMovieService
    {
        private readonly DatabaseContext ctx;
        public MovieService(DatabaseContext ctx)
        {
            this.ctx = ctx;
        }
        public bool Add(Movie model)
        {
            try
            {
                ctx.Movie.Add(model);
                ctx.SaveChanges();
                foreach (int genreId in model.Genres)
                {
                    var movieGenre = new MovieGenre
                    {
                        MovieId = model.Id,
                        GenreId = genreId
                    };
                    ctx.MovieGenre.Add(movieGenre);
                }
                ctx.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                var data = this.GetById(id);
                if(data == null)
                {
                    return false;
                }
                var movieGenres = ctx.MovieGenre.Where(a => a.MovieId == data.Id);
                foreach (var movieGenre in movieGenres)
                {
                    ctx.MovieGenre.Remove(movieGenre);
                }
                ctx.Movie.Remove(data);
                ctx.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public Movie GetById(int id)
        {
            return ctx.Movie.Find(id);
        }

        public MovieListVm List(string term="", bool paging=false, int currentPage=0 )
        {
            var data = new MovieListVm();

            var list = ctx.Movie.ToList();
            if (!string.IsNullOrEmpty(term))
            {
                term=term.ToLower();
                list=list.Where(a=>a.Title.ToLower().StartsWith(term)).ToList();
            }

            if (paging)
            {
                //here we will apply paging
                int pageSize = 5;
                int count = list.Count;
                int TotalPages = (int)Math.Ceiling(count / (double)pageSize);
                list = list.Skip((currentPage-1)*pageSize).Take(pageSize).ToList();
                data.PageSize = pageSize;
                data.CurrentPage = currentPage;
                data.TotalPage = TotalPages;
            }

            foreach (var item in list)
            {
                var genres = (from genre in ctx.Genre join mg in ctx.MovieGenre 
                             on genre.Id equals mg.GenreId 
                             where mg.MovieId == item.Id
                             select genre.GenreName).ToList();
                var genreNames = string.Join(',', genres);
                item.GenreNames = genreNames;
            }
            data.MovieList = list.AsQueryable();
            return data;
        }

        public bool Update(Movie model)
        {
            try
            {
                //these genreIds are not selected by users and still present in movieGenre table corresponding to
                //this movieId. So these ids should be removed.
                var genreToDelete = ctx.MovieGenre.Where(a => a.MovieId == model.Id && !model.Genres.Contains(a.GenreId)).ToList();
                foreach (var mg in genreToDelete)
                {
                    ctx.MovieGenre.Remove(mg);
                }
                foreach (int genId in model.Genres)
                {
                    var movieGenre = ctx.MovieGenre.FirstOrDefault(a => a.MovieId == model.Id && a.GenreId == genId);
                    if (movieGenre == null)
                    {
                        movieGenre = new MovieGenre
                        {
                            GenreId = genId,
                            MovieId = model.Id
                        };
                        ctx.MovieGenre.Add(movieGenre);
                    }
                    //we have to add these genre ids in movieGenre table
                }
                ctx.Movie.Update(model);
                
                ctx.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<int> GetGenreByMovieId(int movieID)
        {
            var genreIds = ctx.MovieGenre.Where(a => a.MovieId == movieID).Select(a => a.GenreId).ToList();
            return genreIds;
        }
    }
}
